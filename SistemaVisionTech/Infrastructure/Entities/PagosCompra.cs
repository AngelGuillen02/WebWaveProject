namespace SistemaVisionTech.Infrastructure.Entities
{
    public class PagosCompra
    {
        public int PagoCompraId { get; set; }
        public int CompraId { get; set; }
        public int MetodoPagoId { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; }
        public Compras Compra { get; set; } = new Compras();
        public MetodosPago MetodoPago { get; set; } = new MetodosPago();
    }
}
