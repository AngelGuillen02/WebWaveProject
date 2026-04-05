using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Acceso.Dtos.Usuarios;

namespace SistemaVisionTech.Features.Acceso.Interfaces
{
    public interface IUsuariosService
    {
        Task<Result<IEnumerable<UsuariosDto>>> ObtenerUsuariosAsync();
        Task<Result<UsuariosDto>> ObtenerUsuarioPorIdAsync(int usuarioId);
        Task<Result<UsuariosDto>> CrearUsuarioAsync(UsuariosCreacionDto dto);
        Task<Result<UsuariosDto>> ActualizarUsuarioAsync(int usuarioId, UsuariosActualizacionDto dto);
        Task<Result> EliminarUsuarioAsync(int usuarioId);
    }
}
