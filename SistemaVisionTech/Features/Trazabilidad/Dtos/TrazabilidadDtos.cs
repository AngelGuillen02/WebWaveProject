namespace SistemaVisionTech.Features.Trazabilidad.Dtos
{
    public class SerieProductoCreacionDto
    {
        public int ProductoId { get; set; }
        public string NumeroSerie { get; set; } = string.Empty;
        public int? CompraDetalleId { get; set; }
    }

    public class SerieProductoResponseDto
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public string NumeroSerie { get; set; } = string.Empty;
        public int? CompraDetalleId { get; set; }
        public int? VentaDetalleId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public class LoteProductoCreacionDto
    {
        public int ProductoId { get; set; }
        public string NumeroLote { get; set; } = string.Empty;
        public DateTime? FechaVencimiento { get; set; }
        public int Cantidad { get; set; }
        public int? CompraDetalleId { get; set; }
    }

    public class LoteProductoResponseDto
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public string NumeroLote { get; set; } = string.Empty;
        public DateTime? FechaVencimiento { get; set; }
        public int Cantidad { get; set; }
        public int? CompraDetalleId { get; set; }
        public bool Activo { get; set; }
    }
}
