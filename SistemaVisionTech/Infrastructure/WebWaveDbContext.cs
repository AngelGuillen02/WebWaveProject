using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Infrastructure.Entities;
using SistemaVisionTech.Infrastructure.Maps;

namespace SistemaVisionTech.Infrastructure
{
    public class WebWaveDbContext : DbContext
    {
        public WebWaveDbContext(DbContextOptions<WebWaveDbContext> options) : base(options)
        {
        }

        public DbSet<Perfiles> Perfiles { get; set; }
        public DbSet<Permisos> Permisos { get; set; }
        public DbSet<Usuarios> Usuarios { get; set; }
        public DbSet<UsuariosPerfiles> UsuariosPerfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(WebWaveDbContext).Assembly);
            modelBuilder.ApplyConfiguration(new PerfilesMap());
            modelBuilder.ApplyConfiguration(new PermisosMap());
            modelBuilder.ApplyConfiguration(new UsuariosMap());
            modelBuilder.ApplyConfiguration(new UsuariosPerfilesMap());

        }
    }
}
