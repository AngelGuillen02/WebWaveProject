using SistemaVisionTech.Features.Auditoria.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Features.Auditoria.Services
{
    public class AuditoriaService : IAuditoriaService
    {
        private readonly WebWaveDbContext _context;

        public AuditoriaService(WebWaveDbContext context)
        {
            _context = context;
        }

        public async Task RegistrarEventoAsync(
            string tipo,
            int entidadId,
            string referencia,
            string? usuario,
            string accion,
            string detalle,
            string? valorAnterior = "-",
            string? valorNuevo = "-",
            string? motivo = null)
        {
            try
            {
                string usuarioNormalizado = string.IsNullOrWhiteSpace(usuario) ? "Administrador" : usuario.Trim();

                var log = new SistemaVisionTech.Infrastructure.Entities.Auditoria
                {
                    Tipo = (tipo ?? "SISTEMA").Trim().ToUpperInvariant(),
                    EntidadId = entidadId,
                    Referencia = string.IsNullOrWhiteSpace(referencia) ? $"Registro #{entidadId}" : referencia.Trim(),
                    Usuario = usuarioNormalizado,
                    Fecha = DateTime.UtcNow,
                    Accion = (accion ?? "MODIFICACIÓN").Trim().ToUpperInvariant(),
                    Detalle = detalle ?? string.Empty,
                    ValorAnterior = string.IsNullOrWhiteSpace(valorAnterior) ? "-" : valorAnterior.Trim(),
                    ValorNuevo = string.IsNullOrWhiteSpace(valorNuevo) ? "-" : valorNuevo.Trim(),
                    Motivo = motivo ?? "Auditoría de operaciones",
                    Activo = true
                };

                _context.Auditoria.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // La auditoría no debe bloquear la operación principal en caso de fallo no crítico
                Console.WriteLine($"[AUDITORIA ERROR]: Error al registrar log: {ex.Message}");
            }
        }
    }
}
