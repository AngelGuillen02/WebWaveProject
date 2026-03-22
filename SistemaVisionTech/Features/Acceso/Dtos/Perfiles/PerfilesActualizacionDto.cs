namespace SistemaVisionTech.Features.Acceso.Dtos.Perfiles
{
    public class PerfilesActualizacionDto
    {
        public string Nombre { get; set; } = string.Empty;
        public List<int> PermisosIds { get; set; } = [];
    }
}
