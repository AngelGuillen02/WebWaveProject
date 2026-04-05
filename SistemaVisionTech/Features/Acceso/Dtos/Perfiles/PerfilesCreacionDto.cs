namespace SistemaVisionTech.Features.Acceso.Dtos.Perfiles
{
    public class PerfilesCreacionDto
    {
        public string Nombre { get; set; } = string.Empty;
        public List<int> PermisosIds { get; set; } = [];

    }
}
