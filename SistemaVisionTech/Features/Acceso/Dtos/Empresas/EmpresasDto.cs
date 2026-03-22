using SistemaVisionTech.Features.Acceso.Dtos.Sucursales;

namespace SistemaVisionTech.Features.Acceso.Dtos.Empresas
{
    public class EmpresasDto
    {
        public int EmpresaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rtn { get; set; } = string.Empty;
        public List<SucursalesDto> Sucursales { get; set; } = [];
    }
}
