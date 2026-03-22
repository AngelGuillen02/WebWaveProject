namespace SistemaVisionTech.Features.Acceso.Dtos.Usuarios
{
    public class UsuariosCreacionDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contraseña { get; set; } = string.Empty;
        public int PerfilId { get; set; }
    }
}
