using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AdvancedMicroservicesSolution.src.ProductService.Services;
using Serilog;

var builder = Host.CreateDefaultBuilder(args)
    .UseSerilog((ctx, lc) => lc.WriteTo.Console())
    .ConfigureServices((ctx, services) =>
    {
      services.Configure<RabbitOptions>(ctx.Configuration.GetSection("RabbitMQ"));
      services.AddSingleton<RabbitMqConsumer>();
      services.AddHostedService<RabbitBackgroundService>();
        });

await builder.RunConsoleAsync();