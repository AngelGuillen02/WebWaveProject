namespace SistemaVisionTech.Features.Acceso.Interfaces
{
    public interface IJwtTokenService
    {
        LoginTokenResult GenerarToken(int usuarioId, string nombre, string email, int perfilId, string perfilNombre);
    }

    public record LoginTokenResult(string Token, DateTime Expira);
}
