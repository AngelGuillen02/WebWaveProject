namespace SistemaVisionTech.Features.Acceso.Dtos.Perfiles
{
    public class PerfilesDto
    {
        public int PerfilId { get; set; }
        public string Nombre { get; set; } = string.Empty;

        public List<string> Permisos { get; set; } = [];
    }
}
