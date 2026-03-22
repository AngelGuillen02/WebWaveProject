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
        public List<VentaDetalleDto> Detalles { get; set; } = [];
        public List<PagoVentaDto> Pagos { get; set; } = [];
    }
}
