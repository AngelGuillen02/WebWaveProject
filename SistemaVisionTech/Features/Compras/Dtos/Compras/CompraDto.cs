using SistemaVisionTech.Features.Compras.Dtos.Pagos;

namespace SistemaVisionTech.Features.Compras.Dtos.Compras
{
    public class CompraDto
    {
        public int CompraId { get; set; }
        public int ProveedorId { get; set; }
        public string Proveedor { get; set; } = string.Empty;
        public DateTime FechaCompra { get; set; }
        public decimal Total { get; set; }
        public string EstadoCompra { get; set; } = string.Empty;
        public List<CompraDetalleDto> Detalles { get; set; } = [];
        public List<PagoCompraDto> Pagos { get; set; } = [];
    }
}
