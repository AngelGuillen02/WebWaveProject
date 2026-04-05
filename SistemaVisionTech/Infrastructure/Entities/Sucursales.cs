namespace SistemaVisionTech.Infrastructure.Entities
{
    public class Sucursales
    {
        public int SucursalId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public int EmpresaId { get; set; }
        public Empresas Empresa { get; set; } = new Empresas();


    }
}
