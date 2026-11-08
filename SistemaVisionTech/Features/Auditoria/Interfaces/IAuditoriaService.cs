namespace SistemaVisionTech.Features.Auditoria.Interfaces
{
    public interface IAuditoriaService
    {
        Task RegistrarEventoAsync(
            string tipo,
            int entidadId,
            string referencia,
            string? usuario,
            string accion,
            string detalle,
            string? valorAnterior = "-",
            string? valorNuevo = "-",
            string? motivo = null
        );
    }
}
