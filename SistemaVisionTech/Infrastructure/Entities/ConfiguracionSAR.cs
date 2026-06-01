namespace SistemaVisionTech.Infrastructure.Entities
{
    public class ConfiguracionSAR
    {
        public int Id { get; set; }
        public int SucursalId { get; set; }
        public string RTN { get; set; } = string.Empty;
        public string CAI { get; set; } = string.Empty;
        public string RangoDesde { get; set; } = string.Empty;
        public string RangoHasta { get; set; } = string.Empty;
        public DateTime FechaLimiteEmision { get; set; }
        public int CorrelativoActual { get; set; } = 0;
        public bool Activo { get; set; } = true;

        // Navegaciones
        public Sucursales? Sucursal { get; set; }
    }
}
