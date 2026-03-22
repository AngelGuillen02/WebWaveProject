namespace SistemaVisionTech.Features.Inventario.Dtos.Inventario
{
    public class InventarioDto
    {
        public int InventarioId { get; set; }
        public int ProductoId { get; set; }
        public string Producto { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public DateTime FechaIngreso { get; set; }
    }
}
