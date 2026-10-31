namespace SistemaVisionTech.Features.Acceso.Dtos.Usuarios
{
    public class UsuariosActualizacionDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Contraseña { get; set; }
        public int PerfilId { get; set; }
    }
}
