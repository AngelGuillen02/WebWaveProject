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

        public LoginTokenResult GenerarToken(int usuarioId, string nombre, string email, int perfilId, string perfilNombre)
        {
            string jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
                ?? _configuration["Jwt:Key"]
                ?? _configuration["Jwt__Key"]
                ?? "S1st3m4V1s10nT3ch_S3cr3tK3y_#2026!_MustBe32Chars+";

            string issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
                ?? _configuration["Jwt:Issuer"]
                ?? _configuration["Jwt__Issuer"]
                ?? "SistemaVisionTech.API";

            string audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                ?? _configuration["Jwt:Audience"]
                ?? _configuration["Jwt__Audience"]
                ?? "SistemaVisionTech.Client";

            string expiresStr = Environment.GetEnvironmentVariable("JWT_EXPIRES_IN_MINUTES")
                ?? _configuration["Jwt:ExpiresInMinutes"]
                ?? _configuration["Jwt__ExpiresInMinutes"]
                ?? "480";

            int expires = int.TryParse(expiresStr, out int exp) ? exp : 480;

            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(jwtKey));
            SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

            Claim[] claims =
            [
                new Claim(JwtRegisteredClaimNames.Sub,   usuarioId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Name,  nombre),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, perfilNombre ?? "Usuario"),
                new Claim("PerfilId", perfilId.ToString())  
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
