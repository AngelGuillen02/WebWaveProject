namespace SistemaVisionTech.Infrastructure.Entities
{
    public class VentasDetalles
    {
        public int VentaDetalleId { get; set; }
        public int VentaId { get; set; }
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Total { get; set; }
        public Ventas Venta { get; set; } = new Ventas();
        public Productos Producto { get; set; } = new Productos();
    }
}
