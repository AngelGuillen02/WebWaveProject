namespace SistemaVisionTech.Infrastructure.Entities
{
    public class Compras
    {
        public int CompraId { get; set; }
        public int ProveedorId { get; set; }
        public DateTime FechaCompra { get; set; } = DateTime.UtcNow;
        public decimal Total { get; set; }
        public int EstadoCompraId { get; set; }
        public Proveedores Proveedor { get; set; } = new Proveedores();
        public EstadosCompra EstadoCompra { get; set; } = new EstadosCompra();
        public ICollection<ComprasDetalles> Detalles { get; set; } = [];
        public ICollection<PagosCompra> Pagos { get; set; } = [];
        public bool Activo { get; set; } = true;
    }
}
