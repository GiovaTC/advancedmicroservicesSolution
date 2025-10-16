using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AdvancedMicroservicesSolution.src.Shared;


namespace AdvancedMicroservicesSolution.src.ApiGateway.Services
{
    public class AuthService : IAuthService
    {
        private readonly JwtSettings _jwt;
        public AuthService(JwtSettings jwt) => _jwt = jwt;
      
        public Task<bool> ValidateUserAsync(string username, string password)
        {
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
}
