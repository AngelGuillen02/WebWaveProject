namespace SistemaVisionTech.Features.Acceso.Dtos.Sucursales
{
    public class SucursalesDto
    {
        public int SucursalId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public int EmpresaId { get; set; } 
        public string Empresa { get; set; } = string.Empty;
    }
}
