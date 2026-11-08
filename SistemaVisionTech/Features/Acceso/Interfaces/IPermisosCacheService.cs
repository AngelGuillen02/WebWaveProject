namespace SistemaVisionTech.Features.Acceso.Interfaces
{
    public interface IPermisosCacheService
    {
        Task<HashSet<string>> ObtenerPermisosPorPerfilAsync(int perfilId);
        void InvalidarPerfil(int perfilId);
        void InvalidarTodo();
        Task<bool> EsUsuarioActivoAsync(int usuarioId);
        void InvalidarUsuario(int usuarioId);
    }
}
