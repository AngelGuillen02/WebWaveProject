namespace SistemaVisionTech.Features.Compras.Dtos.Pagos
{
    public class PagoCompraDto
    {
        public int PagoCompraId { get; set; }
        public int CompraId { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; }
    }
}
