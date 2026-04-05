using Microsoft.IdentityModel.Tokens;
using SistemaVisionTech.Features.Acceso.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SistemaVisionTech.Features.Acceso.Services
{
    /// <summary>
    /// Servicio de responsabilidad única encargado SOLO de generar tokens JWT.
    /// SRP: la generación del token está separada de la lógica de login.
    /// </summary>
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public LoginTokenResult GenerarToken(
            int usuarioId, string nombre, string email, string perfil)
        {
            var jwtKey = _configuration["Jwt:Key"]!;
            var issuer = _configuration["Jwt:Issuer"]!;
            var audience = _configuration["Jwt:Audience"]!;
            var expires = int.Parse(_configuration["Jwt:ExpiresInMinutes"] ?? "480");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,   usuarioId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Name,  nombre),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
                // ClaimTypes.Role habilita [Authorize(Roles = "...")] en los controllers
                new Claim(ClaimTypes.Role, perfil)
            };

            var expiracion = DateTime.UtcNow.AddMinutes(expires);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiracion,
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return new LoginTokenResult(tokenString, expiracion);
        }
    }
}
