using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Caskr.server.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Caskr.server.Services
{
    public interface IAuthService
    {
        Task<string?> LoginAsync(string email, string password);
    }

    public class AuthService(IUsersService usersService, IConfiguration configuration, IKeycloakClient keycloakClient) : IAuthService
    {
        public async Task<string?> LoginAsync(string email, string password)
        {
            var keycloakToken = await keycloakClient.GetTokenAsync(email, password);
            if (keycloakToken is null)
            {
                return null;
            }

            var user = await usersService.GetUserByEmailAsync(email);
            if (user is null)
            {
                return null;
            }

            var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = configuration["Jwt:Issuer"],
                Audience = configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
