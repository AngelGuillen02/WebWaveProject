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
        public string SiguienteFactura { get; set; } = string.Empty;
        public int FacturasDisponibles { get; set; }
        public bool Vigente { get; set; }
    }

    public class CrearConfiguracionSARDto
    {
        public int SucursalId { get; set; } = 1;
        public string RTN { get; set; } = string.Empty;
        public string CAI { get; set; } = string.Empty;
        public string RangoDesde { get; set; } = "000-001-01-00000001";
        public string RangoHasta { get; set; } = "000-001-01-00005000";
        public DateTime FechaLimiteEmision { get; set; } = DateTime.UtcNow.AddYears(1);
        public bool ResetearCorrelativo { get; set; } = false;
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
        public string RangoDesde { get; set; } = string.Empty;
        public string RangoHasta { get; set; } = string.Empty;
        public DateTime? FechaLimiteEmision { get; set; }
    }
}
