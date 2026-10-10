using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Entities;

namespace TenderManagement.Infrastructure.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public (string Token, DateTime ExpiresAt) GenerateToken(User user, string roleName)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var key = jwtSection["Key"]!;
            var issuer = jwtSection["Issuer"];
            var audience = jwtSection["Audience"];
            var expiryMinutes = int.Parse(jwtSection["ExpiryMinutes"] ?? "480");

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, roleName),
                new("roleId", user.RoleId.ToString()),
                new("departmentId", user.DepartmentId?.ToString() ?? string.Empty)
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }
    }
}
