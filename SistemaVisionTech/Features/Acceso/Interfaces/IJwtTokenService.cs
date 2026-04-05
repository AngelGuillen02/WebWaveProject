namespace SistemaVisionTech.Features.Acceso.Interfaces
{
    public interface IJwtTokenService
    {
        /// <summary>
        /// Genera un JWT firmado con los datos del usuario autenticado.
        /// La expiración es configurable via appsettings Jwt:ExpiresInMinutes.
        /// </summary>
        LoginTokenResult GenerarToken(int usuarioId, string nombre, string email, string perfil);
    }

    public record LoginTokenResult(string Token, DateTime Expira);
}
