namespace SistemaVisionTech.Infrastructure.Entities
{
    public class Perfiles
    {
        public int PerfilId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public ICollection<PerfilesPermisos> Permisos { get; set; } = [];
        public ICollection<Usuarios> Usuarios { get; set; } = [];
        public bool Activo { get; set; } = true;
    }
}
