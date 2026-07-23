namespace SistemaVisionTech.Infrastructure.Entities
{
    public class HistorialMovimientoInventario
    {
        public int MovimientoId { get; set; }
        public int InventarioId { get; set; }
        public int Cantidad { get; set; }
        public string TipoMovimiento { get; set; } = string.Empty;
        public DateTime FechaMovimiento { get; set; }
        public string? OrigenTipo { get; set; } // Compra, Venta, Ajuste, Devolucion
        public int? OrigenId { get; set; }
        public Inventarios? Inventario { get; set; }
    }
}
