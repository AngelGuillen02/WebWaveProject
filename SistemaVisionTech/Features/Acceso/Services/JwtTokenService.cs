using Microsoft.IdentityModel.Tokens;
using SistemaVisionTech.Features.Acceso.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SistemaVisionTech.Features.Acceso.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public LoginTokenResult GenerarToken(int usuarioId, string nombre, string email, string perfil)
        {
            string jwtKey = _configuration["Jwt:Key"]!;
            string issuer = _configuration["Jwt:Issuer"]!;
            string audience = _configuration["Jwt:Audience"]!;
            int expires = int.Parse(_configuration["Jwt:ExpiresInMinutes"] ?? "480");

            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(jwtKey));
            SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

            Claim[] claims =
            [
                new Claim(JwtRegisteredClaimNames.Sub,   usuarioId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Name,  nombre),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, perfil)
            ];

            DateTime expiracion = DateTime.UtcNow.AddMinutes(expires);

            JwtSecurityToken token = new(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiracion,
                signingCredentials: creds);

            string tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return new LoginTokenResult(tokenString, expiracion);
        }
    }
}
