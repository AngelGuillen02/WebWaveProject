namespace SistemaVisionTech.Features.Compras.Dtos
{
    public class ProveedorCreacionDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contacto { get; set; } = string.Empty;
        public string? RTN { get; set; }
    }

    public class ProveedorResponseDto
    {
        public int ProveedorId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contacto { get; set; } = string.Empty;
        public string RTN { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}
