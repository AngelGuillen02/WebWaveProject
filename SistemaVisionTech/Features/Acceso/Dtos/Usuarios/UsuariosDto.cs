namespace SistemaVisionTech.Features.Acceso.Dtos.Usuarios
{
    public class UsuariosDto
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int PerfilId { get; set; }
        public string Perfil { get; set; } = string.Empty;

    }
}
