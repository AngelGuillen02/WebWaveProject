using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure
{
    public class WebWaveDbContext : DbContext
    {
        public WebWaveDbContext(DbContextOptions<WebWaveDbContext> options)
            : base(options) { }

        public DbSet<Perfiles> Perfiles { get; set; }
        public DbSet<Permisos> Permisos { get; set; }
        public DbSet<Usuarios> Usuarios { get; set; }
        public DbSet<UsuariosPerfiles> UsuariosPerfiles { get; set; }
        public DbSet<PerfilesPermisos> PerfilesPermisos { get; set; }

        public DbSet<Empresas> Empresas { get; set; }
        public DbSet<Sucursales> Sucursales { get; set; }

        public DbSet<Productos> Productos { get; set; }
        public DbSet<Clientes> Clientes { get; set; }
        public DbSet<Proveedores> Proveedores { get; set; }
        public DbSet<MetodosPago> MetodosPago { get; set; }

        public DbSet<EstadosVenta> EstadosVenta { get; set; }
        public DbSet<Ventas> Ventas { get; set; }
        public DbSet<VentasDetalles> VentasDetalles { get; set; }
        public DbSet<PagosVenta> PagosVenta { get; set; }

        public DbSet<EstadosCompra> EstadosCompra { get; set; }
        public DbSet<Compras> Compras { get; set; }
        public DbSet<ComprasDetalles> ComprasDetalles { get; set; }
        public DbSet<PagosCompra> PagosCompra { get; set; }

        public DbSet<Inventario> Inventario { get; set; }
        public DbSet<HistorialMovimientoInventario> HistorialMovimientoInventario { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(WebWaveDbContext).Assembly);
        }
    }
}