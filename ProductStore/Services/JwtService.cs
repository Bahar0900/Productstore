using Microsoft.IdentityModel.Tokens;
using ProductStore.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ProductStore.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        ClaimsPrincipal? ValidateToken(string token);
    }

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user)
        {
            var keyString = _configuration["Jwt:Key"] ?? "ProductStoreSuperSecretKeyThatIsAtLeast32BytesLong2026!";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserRole", user.Role)
            };

            var days = int.TryParse(_configuration["Jwt:DurationInDays"], out int d) ? d : 7;

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "ProductStoreApp",
                audience: _configuration["Jwt:Audience"] ?? "ProductStoreUsers",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(days),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            var keyString = _configuration["Jwt:Key"] ?? "ProductStoreSuperSecretKeyThatIsAtLeast32BytesLong2026!";
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(keyString);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"] ?? "ProductStoreApp",
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"] ?? "ProductStoreUsers",
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}
