namespace SistemaVisionTech.Features.Acceso.Dtos.Auth
{
    public class LoginRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Contraseña { get; set; } = string.Empty;
    }
}
