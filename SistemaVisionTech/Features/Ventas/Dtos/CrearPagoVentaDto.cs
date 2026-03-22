namespace SistemaVisionTech.Features.Ventas.Dtos
{
    public class CrearPagoVentaDto
    {
        public int VentaId { get; set; }
        public int MetodoPagoId { get; set; }
        public decimal Monto { get; set; }
    }
}
