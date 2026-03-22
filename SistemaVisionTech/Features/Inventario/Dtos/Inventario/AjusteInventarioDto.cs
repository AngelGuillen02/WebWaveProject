namespace SistemaVisionTech.Features.Inventario.Dtos.Inventario
{
    public class AjusteInventarioDto
    {
        public int ProductoId { get; set; }
        public int CantidadNueva { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }
}
