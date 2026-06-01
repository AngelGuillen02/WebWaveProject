namespace SistemaVisionTech.Features.SAR.Dtos
{
    public class ConfiguracionSARDto
    {
        public int Id { get; set; }
        public int SucursalId { get; set; }
        public string RTN { get; set; } = string.Empty;
        public string CAI { get; set; } = string.Empty;
        public string RangoDesde { get; set; } = string.Empty;
        public string RangoHasta { get; set; } = string.Empty;
        public DateTime FechaLimiteEmision { get; set; }
        public int CorrelativoActual { get; set; }
    }

    public class CrearConfiguracionSARDto
    {
        public int SucursalId { get; set; }
        public string RTN { get; set; } = string.Empty;
        public string CAI { get; set; } = string.Empty;
        public string RangoDesde { get; set; } = string.Empty;
        public string RangoHasta { get; set; } = string.Empty;
        public DateTime FechaLimiteEmision { get; set; }
    }

    public class FacturaEmitidaResponseDto
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public string NumeroFactura { get; set; } = string.Empty;
        public string CAI { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public string? RTNCliente { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public decimal MontoExento { get; set; }
        public decimal MontoGravado15 { get; set; }
        public decimal MontoGravado18 { get; set; }
        public decimal ISV15 { get; set; }
        public decimal ISV18 { get; set; }
        public decimal Total { get; set; }
    }
}
