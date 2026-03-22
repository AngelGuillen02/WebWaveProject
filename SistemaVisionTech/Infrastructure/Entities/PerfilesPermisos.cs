namespace SistemaVisionTech.Infrastructure.Entities
{
    public class PerfilesPermisos
    {
        public int PerfilPermisoId { get; set; }
        public int PerfilId { get; set; }
        public int PermisoId { get; set; }

        public Perfiles Perfil { get; set; } = new Perfiles();
        public Permisos Permiso { get; set; } = new Permisos();
    }
}
