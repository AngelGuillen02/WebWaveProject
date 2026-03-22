namespace SistemaVisionTech.Features.Inventario.Dtos.Movimientos
{
    public class MovimientoDto
    {
        public int MovimientoId { get; set; }
        public int InventarioId { get; set; }
        public int ProductoId { get; set; }
        public string Producto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public string TipoMovimiento { get; set; } = string.Empty;
        public DateTime FechaMovimiento { get; set; }
    }
}
