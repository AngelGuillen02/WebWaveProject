namespace SistemaVisionTech.Features.Ventas.Dtos
{
    public class PagoVentaDto
    {
        public int PagoVentaId { get; set; }
        public int VentaId { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; }
    }
}
