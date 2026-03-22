using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Acceso.Dtos.Empresas;
using SistemaVisionTech.Features.Acceso.Dtos.Perfiles;
using SistemaVisionTech.Features.Acceso.Dtos.Permisos;
using SistemaVisionTech.Features.Acceso.Dtos.Sucursales;
using SistemaVisionTech.Features.Acceso.Dtos.Usuarios;

namespace SistemaVisionTech.Features.Acceso.Interfaces
{
    public interface IAccesosService
    {
        // ── Usuarios ──────────────────────────────────────────
        Task<Result<IEnumerable<UsuariosDto>>> ObtenerUsuariosAsync();
        Task<Result<UsuariosDto>> ObtenerUsuarioPorIdAsync(int usuarioId);
        Task<Result<UsuariosDto>> CrearUsuarioAsync(UsuariosCreacionDto dto);
        Task<Result<UsuariosDto>> ActualizarUsuarioAsync(int usuarioId, UsuariosActualizacionDto dto);
        Task<Result> EliminarUsuarioAsync(int usuarioId);

        // ── Perfiles ──────────────────────────────────────────
        Task<Result<IEnumerable<PerfilesDto>>> ObtenerPerfilesAsync();
        Task<Result<PerfilesDto>> ObtenerPerfilPorIdAsync(int perfilId);
        Task<Result<PerfilesDto>> CrearPerfilAsync(PerfilesCreacionDto dto);
        Task<Result<PerfilesDto>> ActualizarPerfilAsync(int perfilId, PerfilesActualizacionDto dto);
        Task<Result> EliminarPerfilAsync(int perfilId);

        // ── Permisos ──────────────────────────────────────────
        Task<Result<IEnumerable<PermisosDto>>> ObtenerPermisosAsync();
        Task<Result<PermisosDto>> CrearPermisoAsync(PermisosCreacionDto dto);
        Task<Result<PermisosDto>> ActualizarPermisoAsync(int permisoId, PermisosActualizacionDto dto);
        Task<Result> EliminarPermisoAsync(int permisoId);

        // ── Empresas ──────────────────────────────────────────
        Task<Result<IEnumerable<EmpresasDto>>> ObtenerEmpresasAsync();
        Task<Result<EmpresasDto>> ObtenerEmpresaPorIdAsync(int empresaId);
        Task<Result<EmpresasDto>> CrearEmpresaAsync(EmpresasCreacionDto dto);
        Task<Result<EmpresasDto>> ActualizarEmpresaAsync(int empresaId, EmpresasActualizacionDto dto);
        Task<Result> EliminarEmpresaAsync(int empresaId);

        // ── Sucursales ────────────────────────────────────────
        Task<Result<IEnumerable<SucursalesDto>>> ObtenerSucursalesAsync();
        Task<Result<SucursalesDto>> ObtenerSucursalPorIdAsync(int sucursalId);
        Task<Result<SucursalesDto>> CrearSucursalAsync(SucursalesCreacionDto dto);
        Task<Result<SucursalesDto>> ActualizarSucursalAsync(int sucursalId, SucursalesActualizacionDto dto);
        Task<Result> EliminarSucursalAsync(int sucursalId);
    }
}