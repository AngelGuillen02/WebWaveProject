namespace SistemaVisionTech.Features.Compras.Dtos.Compras
{
    public class CompraDetalleDto
    {
        public int CompraDetalleId { get; set; }
        public int ProductoId { get; set; }
        public string Producto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Total { get; set; }
    }
}
