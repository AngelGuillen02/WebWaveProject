namespace SistemaVisionTech.Infrastructure.Entities
{
    public class Usuarios
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contraseña { get; set; } = string.Empty;
        public int PerfilId { get; set; }
        public Perfiles Perfil { get; set; } = new Perfiles();
        public bool Activo { get; set; } = true;

    }
}
