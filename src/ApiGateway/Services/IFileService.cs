namespace AdvancedMicroservicesSolution.src.ApiGateway.Services
{
    public interface IFileService
    {
        Task<string> SaveFileAsync(IFormFile file);
    }
}
