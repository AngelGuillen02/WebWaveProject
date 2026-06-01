namespace SistemaVisionTech.Infrastructure.Entities
{
    public class ComprasDetalles
    {
        public int CompraDetalleId { get; set; }
        public int CompraId { get; set; }
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Total { get; set; }
        public Compra Compra { get; set; } = new Compra();
        public Producto Producto { get; set; } = new Producto();
    }
}
