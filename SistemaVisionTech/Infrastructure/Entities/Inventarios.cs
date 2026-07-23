namespace SistemaVisionTech.Infrastructure.Entities
{
    public class Inventarios
    {
        public int InventarioId { get; set; }
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public DateTime FechaIngreso { get; set; }
        public Producto? Producto { get; set; }
        public ICollection<HistorialMovimientoInventario> Movimientos { get; set; } = [];
    }
}
