using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AisaacSTR.Data.Entities;
using Microsoft.IdentityModel.Tokens;

namespace AisaacSTR.Services
{
    public interface IJwtService
    {
        string GenerateToken(Usuario usuario);
    }

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(Usuario usuario)
        {
            var keyString = _config["Jwt:Key"] ?? "AISAAC_STR_SUPER_SECRET_KEY_2026_JWT_RETAIL_SECURITY_TOKEN!";
            var issuer = _config["Jwt:Issuer"] ?? "AisaacSTR";
            var audience = _config["Jwt:Audience"] ?? "AisaacSTR_Clients";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("Telefono", usuario.Telefono ?? ""),
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
