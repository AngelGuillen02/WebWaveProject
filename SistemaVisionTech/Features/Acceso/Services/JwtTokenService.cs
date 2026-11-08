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
            string? configuredKey = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(configuredKey) || configuredKey.StartsWith("[CONFIGURAR"))
            {
                configuredKey = null;
            }

            string? jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
                ?? Environment.GetEnvironmentVariable("Jwt__Key")
                ?? configuredKey
                ?? _configuration["Jwt__Key"];

            if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
            {
                throw new InvalidOperationException("CRÍTICO DE SEGURIDAD: La clave de firma JWT (JWT_KEY) no está configurada o tiene menos de 32 caracteres.");
            }

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
                ?? "120"; // 2 horas (reducido de 8 horas por seguridad)

            int expires = int.TryParse(expiresStr, out int exp) ? exp : 480;

            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(jwtKey));
            SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

            List<Claim> claims =
            [
                new Claim(JwtRegisteredClaimNames.Sub,   usuarioId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Name,  nombre),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, perfilNombre ?? "Usuario"),
                new Claim("PerfilId", perfilId.ToString())  
            ];

            if (!string.IsNullOrEmpty(perfilNombre))
            {
                if (perfilNombre.Contains("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                    claims.Add(new Claim(ClaimTypes.Role, "SuperUsuario"));
                }
                if (perfilNombre.Contains("Venta", StringComparison.OrdinalIgnoreCase) ||
                    perfilNombre.Contains("Factur", StringComparison.OrdinalIgnoreCase))
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Facturador"));
                    claims.Add(new Claim(ClaimTypes.Role, "Ventas"));
                }
                if (perfilNombre.Contains("Cajer", StringComparison.OrdinalIgnoreCase))
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Cajero"));
                }
                if (perfilNombre.Contains("Almac", StringComparison.OrdinalIgnoreCase) ||
                    perfilNombre.Contains("Inventar", StringComparison.OrdinalIgnoreCase))
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Almacen"));
                    claims.Add(new Claim(ClaimTypes.Role, "EncargadoInventario"));
                }
            }

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
