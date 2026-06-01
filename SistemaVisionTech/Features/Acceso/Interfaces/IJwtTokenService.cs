namespace SistemaVisionTech.Features.Acceso.Interfaces
{
    public interface IJwtTokenService
    {
        LoginTokenResult GenerarToken(int usuarioId, string nombre, string email, string perfil);
    }

    public record LoginTokenResult(string Token, DateTime Expira);
}
