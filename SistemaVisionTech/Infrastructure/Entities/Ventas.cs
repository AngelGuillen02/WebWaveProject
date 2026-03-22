namespace SistemaVisionTech.Infrastructure.Entities
{
    public class Ventas
    {
        public int VentaId { get; set; }
        public int ClienteId { get; set; }
        public DateTime FechaVenta { get; set; }
        public decimal Total { get; set; }
        public int EstadoVentaId { get; set; }
        public Clientes Cliente { get; set; } = new Clientes();
        public EstadosVenta EstadoVenta { get; set; } = new EstadosVenta();
        public ICollection<VentasDetalles> Detalles { get; set; } = [];
        public ICollection<PagosVenta> Pagos { get; set; } = [];
    }
}
