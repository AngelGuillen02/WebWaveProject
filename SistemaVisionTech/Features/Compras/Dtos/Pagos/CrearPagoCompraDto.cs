namespace SistemaVisionTech.Features.Compras.Dtos.Pagos
{
    public class CrearPagoCompraDto
    {
        public int CompraId { get; set; }
        public int MetodoPagoId { get; set; }
        public decimal Monto { get; set; }
    }
}
