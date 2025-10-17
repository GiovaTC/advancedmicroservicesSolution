//using AdvancedMicroservicesSolution.src.ApiGateway.DTOs;

namespace AdvancedMicroservicesSolution.src.ApiGateway.Services
{
    public interface IAuthService
    {
        string GenerateToken(string userId, string role);
        Task<bool> ValidateUserAsync(string username, string password);
    }
}
