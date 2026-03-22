namespace SistemaVisionTech.Infrastructure.Entities
{
    public class HistorialMovimientoInventario
    {
        public int MovimientoId { get; set; }
        public int InventarioId { get; set; }
        public int Cantidad { get; set; }
        public string TipoMovimiento { get; set; } = string.Empty;
        public DateTime FechaMovimiento { get; set; }
        public Inventario Inventario { get; set; } = new Inventario();   
    }
}
