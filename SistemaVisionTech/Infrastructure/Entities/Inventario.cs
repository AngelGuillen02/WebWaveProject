namespace SistemaVisionTech.Infrastructure.Entities
{
    public class Inventario
    {
        public int InventarioId { get; set; }
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public DateTime FechaIngreso { get; set; }
        public Productos Producto { get; set; } = new Productos();
        public ICollection<HistorialMovimientoInventario> Movimientos { get; set; } = [];
    }
}
