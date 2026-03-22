namespace SistemaVisionTech.Features.Ventas.Dtos
{
    public class VentaDetalleDto
    {
        public int VentaDetalleId { get; set; }
        public int ProductoId { get; set; }
        public string Producto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Total { get; set; }
    }
}
