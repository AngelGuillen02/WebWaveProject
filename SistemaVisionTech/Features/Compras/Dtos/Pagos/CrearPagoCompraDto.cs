namespace SistemaVisionTech.Features.Compras.Dtos
{
    public class CrearPagoCompraDto
    {
        public int CompraId { get; set; }
        public int MetodoPagoId { get; set; }
        public decimal Monto { get; set; }
    }
}
