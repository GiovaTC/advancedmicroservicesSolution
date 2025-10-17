using Microsoft.AspNetCore.Mvc;
using AdvancedMicroservicesSolution.src.ApiGateway.Services;


namespace AdvancedMicroservicesSolution.src.ApiGateway.Controllers
{
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
}
