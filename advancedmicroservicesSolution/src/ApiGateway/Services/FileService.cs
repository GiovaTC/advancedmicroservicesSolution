using Microsoft.AspNetCore.WebUtilities;

namespace AdvancedMicroservicesSolution.src.ApiGateway.Services
{
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
        // Implementación de los métodos de IFileService aquí
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
}
