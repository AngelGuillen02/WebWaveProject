using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Acceso.Dtos.Usuarios;

namespace SistemaVisionTech.Features.Acceso.Interfaces
{
    public interface IUsuariosService
    {
        Task<Result<IEnumerable<UsuariosDto>>> ObtenerUsuariosAsync(bool esAdmin = false);
        Task<Result<UsuariosDto>> ObtenerUsuarioPorIdAsync(int usuarioId, bool esAdmin = false);
        Task<Result<UsuariosDto>> CrearUsuarioAsync(UsuariosCreacionDto dto, string? usuarioActual = null);
        Task<Result<UsuariosDto>> ActualizarUsuarioAsync(int usuarioId, UsuariosActualizacionDto dto, bool esAdmin = false, string? usuarioActual = null);
        Task<Result> EliminarUsuarioAsync(int usuarioId, string? usuarioActual = null);
    }
}
