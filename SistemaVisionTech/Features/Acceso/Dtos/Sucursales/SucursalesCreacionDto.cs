namespace SistemaVisionTech.Features.Acceso.Dtos.Sucursales
{
    public class SucursalesCreacionDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public int EmpresaId { get; set; }
    }
}
