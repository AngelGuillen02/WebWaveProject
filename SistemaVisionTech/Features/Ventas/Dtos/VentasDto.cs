namespace SistemaVisionTech.Features.Ventas.Dtos
{
    public class VentasDto
    {
        public int VentaId { get; set; }
        public int ClienteId { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public DateTime FechaVenta { get; set; }
        public decimal Total { get; set; }
        public string EstadoVenta { get; set; } = string.Empty;
        public bool EsCotizacion { get; set; }
        public string? NumeroCotizacion { get; set; }
        public string? NumeroFactura { get; set; }
        public string? CAI { get; set; }
        public decimal? ISV15 { get; set; }
        public decimal? ISV18 { get; set; }
        public decimal? MontoExento { get; set; }
        public decimal? MontoGravado15 { get; set; }
        public DateTime? FechaFactura { get; set; }
        public List<VentaDetalleDto> Detalles { get; set; } = [];
        public List<PagoVentaDto> Pagos { get; set; } = [];
    }
}
