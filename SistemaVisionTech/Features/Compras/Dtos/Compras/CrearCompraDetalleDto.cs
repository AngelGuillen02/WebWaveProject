namespace SistemaVisionTech.Features.Compras.Dtos
{
    public class CrearCompraDetalleDto
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }
}
