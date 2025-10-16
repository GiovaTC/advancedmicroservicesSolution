using AdvancedMicroservicesSolution.src.Shared;
using AdvancedMicroservicesSolution.src.ApiGateway.Services;
using Xunit;

public class AuthServiceTests
{
    public object Assert { get; private set; }

    [Fact]
    public void GenerateToken_ReturnsNotNull()
    {
        var jwt = new JwtSettings { Secret = "test_secret_which_is_long_enough", Issuer = "i", Audience = "a", ExpiryMinutes = 60 };
        var svc = new AuthService(jwt);
        var token = svc.GenerateToken("user1", "User");
        object value = Assert.False(string.IsNullOrEmpty(token));
    }
}