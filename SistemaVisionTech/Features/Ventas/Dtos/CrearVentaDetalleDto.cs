namespace SistemaVisionTech.Features.Ventas.Dtos
{
    public class CrearVentaDetalleDto
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; } = 0;
        public decimal Descuento { get; set; } = 0;
    }
}
