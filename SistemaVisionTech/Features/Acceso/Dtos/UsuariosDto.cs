namespace SistemaVisionTech.Features.Acceso.Dtos
{
    public class UsuariosDto
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Contraseña { get; set; }
        public int PerfilId { get; set; }

    }
}
