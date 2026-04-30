namespace SistemaVisionTech.Infrastructure.Entities
{
    public class Permisos
    {
        public int PermisoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public ICollection<PerfilesPermisos> Perfiles { get; set; } = [];
        public bool Activo { get; set; } = true;

    }
}
