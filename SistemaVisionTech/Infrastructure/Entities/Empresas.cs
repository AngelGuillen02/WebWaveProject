namespace SistemaVisionTech.Infrastructure.Entities
{
    public class Empresas
    {
        public int EmpresaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rtn { get; set; } = string.Empty;
        public ICollection<Sucursales> Sucursales { get; set; } = [];
    }
}
