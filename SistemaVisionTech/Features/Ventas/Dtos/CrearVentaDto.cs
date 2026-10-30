namespace SistemaVisionTech.Features.Ventas.Dtos
{
    public class CrearVentaDto
    {
        public int ClienteId { get; set; }
        public int SucursalId { get; set; } = 1;
        public int? MetodoPagoId { get; set; } = 1; // 1 = Efectivo
        public bool ConcretarVenta { get; set; } = true;
        public bool EmitirFactura { get; set; } = true;
        public List<CrearVentaDetalleDto> Detalles { get; set; } = [];
    }
}
