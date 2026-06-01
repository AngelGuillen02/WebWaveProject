namespace SistemaVisionTech.Features.Productos.Dtos
{
    public class ProductoCreacionDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public bool TieneNumeroSerie { get; set; } = false;
        public bool TieneLote { get; set; } = false;
        public string? CodigoBarras { get; set; }
        public string TipoISV { get; set; } = "ISV15";
    }

    public class ProductoResponseDto
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public bool TieneNumeroSerie { get; set; }
        public bool TieneLote { get; set; }
        public string? CodigoBarras { get; set; }
        public string TipoISV { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}
