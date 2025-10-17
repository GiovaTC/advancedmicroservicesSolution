# advancedmicroservicesSolution .
# AdvancedMicroservicesSolution :

<img width="1024" height="1024" alt="image" src="https://github.com/user-attachments/assets/8bdff576-3522-45be-937a-78acb4ffdd36" />    

# AdvancedMicroservicesSolution

A scaffolded solution for **.NET 8** with:
- JWT Authentication (ASP.NET Core Web API)
- File upload with size & MIME type validation
- Microservice (ProductService) communicating via **RabbitMQ** (publish/subscribe)
- Integration notes for **Angular** / **React** frontend
- Unit test project using xUnit
- DI (services + repositories), Serilog logging, appsettings.json configuration

---

## Solution structure

```
AdvancedMicroservicesSolution/
├── src/
│   ├── ApiGateway/
│   │   ├── ApiGateway.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── FilesController.cs
│   │   │   └── ProductsController.cs
│   │   ├── Services/
│   │   │   ├── IAuthService.cs
│   │   │   ├── AuthService.cs
│   │   │   ├── IFileService.cs
│   │   │   └── FileService.cs
│   │   ├── Messaging/
│   │   │   └── RabbitMqPublisher.cs
│   │   └── DTOs/
│   ├── ProductService/
│   │   ├── ProductService.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Services/
│   │   │   └── RabbitMqConsumer.cs
│   │   └── Controllers/ (optional gRPC service omitted)
│   └── Shared/
│       ├── JwtSettings.cs
│       └── Models/
├── tests/
│   └── ApiGateway.Tests/
│       ├── ApiGateway.Tests.csproj
│       └── AuthServiceTests.cs
├── docker-compose.yml
└── README.md
```

---

> The files below are **excerpts** and ready to paste into your Visual Studio 2022/2023 (.NET 8 SDK required). Replace namespaces if you prefer.

---

## src/ApiGateway/Program.cs

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using AdvancedMicroservicesSolution.Shared;
using ApiGateway.Services;
using ApiGateway.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console());

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();

// Services
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddSingleton<RabbitMqPublisher>();

builder.Services.AddControllers();

// JWT
var key = Encoding.ASCII.GetBytes(jwtSettings.Secret);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## src/ApiGateway/appsettings.json

```json
{
  "Jwt": {
    "Secret": "super_secret_key_change_this_to_env_var_very_long",
    "Issuer": "AdvancedApi",
    "Audience": "AdvancedClients",
    "ExpiryMinutes": 60
  },
  "AllowedFileTypes": ["image/png", "image/jpeg", "application/pdf"],
  "MaxFileSizeBytes": 5242880,
  "RabbitMq": {
    "Host": "localhost",
    "User": "guest",
    "Password": "guest",
    "Exchange": "advanced.exchange"
  }
}
```

---

## src/Shared/JwtSettings.cs

```csharp
namespace AdvancedMicroservicesSolution.Shared;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; }
}
```

---

## src/ApiGateway/Services/IAuthService.cs

```csharp
using ApiGateway.DTOs;

namespace ApiGateway.Services;

public interface IAuthService
{
    string GenerateToken(string userId, string role);
    Task<bool> ValidateUserAsync(string username, string password);
}
```

## src/ApiGateway/Services/AuthService.cs

```csharp
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AdvancedMicroservicesSolution.Shared;

namespace ApiGateway.Services;

public class AuthService : IAuthService
{
    private readonly JwtSettings _jwt;
    public AuthService(JwtSettings jwt) => _jwt = jwt;

    public Task<bool> ValidateUserAsync(string username, string password)
    {
        // Replace with real user store (EF Core, Identity, etc.)
        var valid = username == "admin" && password == "P@ssw0rd";
        return Task.FromResult(valid);
    }

    public string GenerateToken(string userId, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwt.Secret);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes),
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
```

---

## src/ApiGateway/Controllers/AuthController.cs

```csharp
using Microsoft.AspNetCore.Mvc;
using ApiGateway.Services;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var ok = await _auth.ValidateUserAsync(req.Username, req.Password);
        if (!ok) return Unauthorized();
        var token = _auth.GenerateToken(req.Username, "User");
        return Ok(new { token });
    }
}

public record LoginRequest(string Username, string Password);
```

---

## src/ApiGateway/Services/IFileService.cs

```csharp
namespace ApiGateway.Services;

public interface IFileService
{
    Task<string> SaveFileAsync(IFormFile file);
}
```

## src/ApiGateway/Services/FileService.cs

```csharp
using Microsoft.AspNetCore.WebUtilities;

namespace ApiGateway.Services;

public class FileService : IFileService
{
    private readonly IConfiguration _config;
    private readonly long _maxBytes;
    private readonly string[] _allowedTypes;

    public FileService(IConfiguration config)
    {
        _config = config;
        _maxBytes = _config.GetValue<long>("MaxFileSizeBytes");
        _allowedTypes = _config.GetSection("AllowedFileTypes").Get<string[]>();
    }

    public async Task<string> SaveFileAsync(IFormFile file)
    {
        if (file == null) throw new ArgumentNullException(nameof(file));
        if (file.Length == 0) throw new ArgumentException("Empty file");
        if (file.Length > _maxBytes) throw new InvalidOperationException($"File too large. Max: {_maxBytes} bytes");
        if (!_allowedTypes.Contains(file.ContentType)) throw new InvalidOperationException("Invalid file type");

        var fileName = Path.GetRandomFileName() + Path.GetExtension(file.FileName);
        var savePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
        Directory.CreateDirectory(savePath);
        var full = Path.Combine(savePath, fileName);

        await using var stream = System.IO.File.Create(full);
        await file.CopyToAsync(stream);
        return fileName;
    }
}
```

---

## src/ApiGateway/Controllers/FilesController.cs

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiGateway.Services;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;

    public FilesController(IFileService fileService) => _fileService = fileService;

    [HttpPost]
    [Authorize]
    [RequestSizeLimit(10_485_760)] // 10 MB as an extra safety
    public async Task<IActionResult> Upload(IFormFile file)
    {
        try
        {
            var saved = await _fileService.SaveFileAsync(file);
            return Ok(new { fileName = saved });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
```

---

## src/ApiGateway/Messaging/RabbitMqPublisher.cs

```csharp
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace ApiGateway.Messaging;

public class RabbitMqPublisher : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IConfiguration _config;

    public RabbitMqPublisher(IConfiguration config)
    {
        _config = config;
        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMq:Host"],
            UserName = _config["RabbitMq:User"],
            Password = _config["RabbitMq:Password"]
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(exchange: _config["RabbitMq:Exchange"], type: ExchangeType.Fanout, durable: true);
    }

    public void Publish<T>(T message, string routingKey = "")
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);
        _channel.BasicPublish(exchange: _config["RabbitMq:Exchange"], routingKey: routingKey, basicProperties: null, body: body);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
```

---

## src/ProductService/Program.cs (RabbitMQ consumer)

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ProductService.Services;
using Serilog;

var builder = Host.CreateDefaultBuilder(args)
    .UseSerilog((ctx, lc) => lc.WriteTo.Console())
    .ConfigureServices((ctx, services) =>
    {
        services.Configure<RabbitOptions>(ctx.Configuration.GetSection("RabbitMq"));
        services.AddSingleton<RabbitMqConsumer>();
        services.AddHostedService<RabbitBackgroundService>();
    });

await builder.RunConsoleAsync();
```

## src/ProductService/Services/RabbitMqConsumer.cs

```csharp
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ProductService.Services;

public class RabbitOptions { public string Host { get; set; } = "localhost"; public string Exchange { get; set; } = "advanced.exchange"; public string User { get; set; } = "guest"; public string Password { get; set; } = "guest"; }

public class RabbitMqConsumer
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly RabbitOptions _options;

    public RabbitMqConsumer(IOptions<RabbitOptions> opts)
    {
        _options = opts.Value;
        var factory = new ConnectionFactory { HostName = _options.Host, UserName = _options.User, Password = _options.Password };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_options.Exchange, ExchangeType.Fanout, durable: true);
        var q = _channel.QueueDeclare().QueueName;
        _channel.QueueBind(q, _options.Exchange, "");

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (sender, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            Console.WriteLine("Received: " + json);
            // process message
        };
        _channel.BasicConsume(queue: q, autoAck: true, consumer: consumer);
    }
}

public class RabbitBackgroundService : BackgroundService
{
    private readonly RabbitMqConsumer _consumer;
    public RabbitBackgroundService(RabbitMqConsumer consumer) => _consumer = consumer;
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
```

---

## tests/ApiGateway.Tests/AuthServiceTests.cs

```csharp
using Xunit;
using ApiGateway.Services;
using AdvancedMicroservicesSolution.Shared;

public class AuthServiceTests
{
    [Fact]
    public void GenerateToken_ReturnsNotNull()
    {
        var jwt = new JwtSettings { Secret = "test_secret_which_is_long_enough", Issuer = "i", Audience = "a", ExpiryMinutes = 60 };
        var svc = new AuthService(jwt);
        var token = svc.GenerateToken("user1", "User");
        Assert.False(string.IsNullOrEmpty(token));
    }
}
```

---

## Frontend integration (Angular / React) — quick guide

### Authentication / Login (common)
1. POST `/api/auth/login` with `{ username, password }`.
2. Receive `{ token }` and store in `localStorage` or `sessionStorage`.
3. For API calls, attach header `Authorization: Bearer <token>`.

### Angular example (HttpInterceptor)
- Create an `AuthInterceptor` to append the Authorization header.

### File upload
- Use `FormData` in frontend, append file under key `file`.
- POST to `/api/files` with `Authorization` header.

### Example Angular snippet
```ts
const fd = new FormData();
fd.append('file', file);
this.http.post('/api/files', fd, { headers: new HttpHeaders({ 'Authorization': `Bearer ${token}` }) });
```

---

## Docker Compose (development)

```yaml
version: '3.8'
services:
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - '5672:5672'
      - '15672:15672'
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
  apigateway:
    build: ./src/ApiGateway
    ports:
      - '5000:80'
    depends_on:
      - rabbitmq
  productservice:
    build: ./src/ProductService
    depends_on:
      - rabbitmq
```

---

## Security notes & next steps
- Move `Jwt:Secret` to environment variables or Secret Manager in production.
- Replace the simple `AuthService` with ASP.NET Core Identity or an external identity provider.
- Add database persistence (EF Core) with migrations for users, files metadata and products.
- Replace manual file storage with cloud storage (S3, Azure Blob) for scalability.
- For gRPC: add a `proto` contract and host a gRPC service in `ProductService` instead of or alongside RabbitMQ.

---

## README.md (short)

Include run instructions: install .NET 8, run docker-compose up to start RabbitMQ, open ApiGateway project in Visual Studio and run.

---

*If you want, I can:*
- generate full files ready to paste (one archive), or
- add EF Core + migrations, or
- swap RabbitMQ for gRPC with example proto and client stubs.


<!-- End of scaffold -->

---

## EF Core + Oracle 19c — persistencia (añadido)

A continuación agregué un ejemplo mínimo para integrar **Entity Framework Core** con **Oracle 19c**, incluyendo `DbContext`, entidad `Product`, repositorio, configuración en `Program.cs`, ejemplo de `appsettings.json` (cadena de conexión Oracle) y comandos para crear migraciones.

> Nota: para Oracle hay varios paquetes posibles. En este scaffold recomiendo instalar:
>
> - `Oracle.EntityFrameworkCore` (proveedor EF Core oficial de Oracle)
> - `Oracle.ManagedDataAccess.Core` (driver managed para Oracle)
> - `Microsoft.EntityFrameworkCore.Design` (herramientas de diseño y migraciones)

### src/Shared/Models/Product.cs

```csharp
namespace AdvancedMicroservicesSolution.Shared.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### src/ApiGateway/Data/AppDbContext.cs

```csharp
using Microsoft.EntityFrameworkCore;
using AdvancedMicroservicesSolution.Shared.Models;

namespace ApiGateway.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).IsRequired().HasMaxLength(200);
            b.Property(p => p.Price).HasColumnType("DECIMAL(18,2)");
            b.Property(p => p.CreatedAt).HasDefaultValueSql("SYSDATE"); // Oracle SYSDATE
        });
    }
}
```

### src/ApiGateway/Repositories/IProductRepository.cs

```csharp
using AdvancedMicroservicesSolution.Shared.Models;

namespace ApiGateway.Repositories;

public interface IProductRepository
{
    Task<Product> AddAsync(Product product);
    Task<Product?> GetByIdAsync(int id);
    Task<IList<Product>> ListAsync();
}
```

### src/ApiGateway/Repositories/ProductRepository.cs

```csharp
using Microsoft.EntityFrameworkCore;
using ApiGateway.Data;
using AdvancedMicroservicesSolution.Shared.Models;

namespace ApiGateway.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;
    public ProductRepository(AppDbContext db) => _db = db;

    public async Task<Product> AddAsync(Product product)
    {
        var ent = (await _db.Products.AddAsync(product)).Entity;
        await _db.SaveChangesAsync();
        return ent;
    }

    public Task<Product?> GetByIdAsync(int id) => _db.Products.FirstOrDefaultAsync(p => p.Id == id);

    public Task<IList<Product>> ListAsync() => _db.Products.OrderByDescending(p => p.CreatedAt).ToListAsync() as Task<IList<Product>> ?? Task.FromResult<IList<Product>>(new List<Product>());
}
```

### Cambios en Program.cs (ApiGateway) — registrar DbContext y repositorio

En `src/ApiGateway/Program.cs` (ya existente) agregue la configuración para EF Core:

```csharp
using Microsoft.EntityFrameworkCore;
using ApiGateway.Data;
using ApiGateway.Repositories;

// (después de builder.Configuration...)
var oracleConn = builder.Configuration.GetConnectionString("Oracle");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    // UseOracle viene del paquete Oracle.EntityFrameworkCore
    options.UseOracle(oracleConn);
});

builder.Services.AddScoped<IProductRepository, ProductRepository>();
```

### appsettings.json (ejemplo con cadena Oracle)

```json
{
  "ConnectionStrings": {
    "Oracle": "User Id=MYUSER;Password=MYPASSWORD;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ORCLCDB)))"
  }
}
```

> Ajusta `HOST`, `PORT`, y `SERVICE_NAME` según tu instalación de Oracle 19c. Alternativamente puedes usar `HOST:PORT/SERVICE_NAME` con algunos drivers, pero la forma mostrada es la más explícita y fiable.

### Dependencias a añadir en `src/ApiGateway/ApiGateway.csproj`

```xml
<ItemGroup>
  <PackageReference Include="Oracle.EntityFrameworkCore" Version="*" />
  <PackageReference Include="Oracle.ManagedDataAccess.Core" Version="*" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="*" />
</ItemGroup>
```

(Reemplaza `*` por la versión estable que prefieras. `dotnet add package <name>` autoselecciona la última si la omites.)

### Crear migraciones y actualizar la base de datos (CLI)

1. Instala la herramienta `dotnet-ef` si no la tienes:

```bash
dotnet tool install --global dotnet-ef
```

2. Desde la carpeta raíz del repositorio ejecuta (ejemplo apuntando al proyecto ApiGateway):

```bash
cd AdvancedMicroservicesSolution
# Crear migración
dotnet ef migrations add InitialCreate --project src/ApiGateway --startup-project src/ApiGateway

# Aplicar migración a la base de datos Oracle
dotnet ef database update --project src/ApiGateway --startup-project src/ApiGateway
```

> Si el proveedor Oracle requiere pasos extra (por ejemplo privilegios, creación previa de tablespace o usuario), asegúrate de conectar con un usuario con permisos para crear tablas o solicita al DBA. Para desarrollo local suele crearse un usuario con su propio tablespace.

### Consideraciones y notas finales

- En Oracle, los tipos de datos y la creación automática de columnas identidad pueden comportarse distinto a SQL Server; revisa las migraciones generadas antes de aplicar a producción.
- Si tu versión del proveedor no soporta alguna extensión de EF Core 8, prueba a usar el paquete `Oracle.EntityFrameworkCore` compatible con la versión de EF Core que tienes instalada.
- Para producción: usa `Secret` y credenciales en variables de entorno o Secret Manager; no guardes contraseñas en `appsettings.json`.

--- :. 
© 2025 Giovanny Alejandro Tapiero Cataño & ChatGPT. Todos los derechos reservados
