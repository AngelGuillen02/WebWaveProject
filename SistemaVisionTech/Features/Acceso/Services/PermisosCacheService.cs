using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SistemaVisionTech.Features.Acceso.Interfaces;
using SistemaVisionTech.Infrastructure;

namespace SistemaVisionTech.Features.Acceso.Services
{
    public class PermisosCacheService : IPermisosCacheService
    {
        private readonly IDbContextFactory<WebWaveDbContext> _dbFactory;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5); 

        public PermisosCacheService(IDbContextFactory<WebWaveDbContext> dbFactory, IMemoryCache cache)
        {
            _dbFactory = dbFactory;
            _cache = cache;
        }

        private static string ClavePerfil(int perfilId) => $"permisos:perfil:{perfilId}";

        public async Task<HashSet<string>> ObtenerPermisosPorPerfilAsync(int perfilId)
        {
            string clave = ClavePerfil(perfilId);

            if (_cache.TryGetValue(clave, out HashSet<string>? permisosCacheados) && permisosCacheados is not null)
                return permisosCacheados;

            await using WebWaveDbContext context = await _dbFactory.CreateDbContextAsync();

            HashSet<string> permisos = await context.PerfilesPermisos
                .AsNoTracking()
                .Where(pp => pp.PerfilId == perfilId)
                .Select(pp => pp.Permiso.Nombre)
                .ToHashSetAsync();

            _cache.Set(clave, permisos, Ttl);
            return permisos;
        }

        // Se llama automáticamente cuando se editan/eliminan permisos de un perfil
        public void InvalidarPerfil(int perfilId) => _cache.Remove(ClavePerfil(perfilId));

        // Útil si algún día editas Permisos globalmente (nombre de un permiso, etc.)
        public void InvalidarTodo()
        {
            if (_cache is MemoryCache mc) mc.Compact(1.0); // limpia todo
        }

        public async Task<bool> EsUsuarioActivoAsync(int usuarioId)
        {
            string clave = $"usuario:activo:{usuarioId}";
            if (_cache.TryGetValue(clave, out bool activo))
                return activo;

            await using var context = await _dbFactory.CreateDbContextAsync();
            bool estaActivo = await context.Usuarios
                .AsNoTracking()
                .Where(u => u.UsuarioId == usuarioId)
                .Select(u => u.Activo)
                .FirstOrDefaultAsync();

            _cache.Set(clave, estaActivo, TimeSpan.FromMinutes(2));
            return estaActivo;
        }

        public void InvalidarUsuario(int usuarioId)
        {
            _cache.Remove($"usuario:activo:{usuarioId}");
        }
    }
}