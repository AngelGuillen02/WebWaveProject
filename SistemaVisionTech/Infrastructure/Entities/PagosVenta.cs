namespace SistemaVisionTech.Infrastructure.Entities
{
    public class PagosVenta
    {
        public int PagoVentaId { get; set; }
        public int VentaId { get; set; }
        public int MetodoPagoId { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; } = DateTime.UtcNow;
        public Venta? Venta { get; set; }
        public MetodosPago? MetodoPago { get; set; }
    }
}
