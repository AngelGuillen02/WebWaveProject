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

        public DbSet<Producto> Productos { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Proveedores> Proveedores { get; set; }
        public DbSet<MetodosPago> MetodosPago { get; set; }

        public DbSet<EstadosVenta> EstadosVenta { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<VentasDetalles> VentasDetalles { get; set; }
        public DbSet<PagosVenta> PagosVenta { get; set; }

        public DbSet<EstadosCompra> EstadosCompra { get; set; }
        public DbSet<Compra> Compras { get; set; }
        public DbSet<ComprasDetalles> ComprasDetalles { get; set; }
        public DbSet<PagosCompra> PagosCompra { get; set; }

        public DbSet<Inventarios> Inventario { get; set; }
        public DbSet<HistorialMovimientoInventario> HistorialMovimientoInventario { get; set; }

        public DbSet<SeriesProducto> SeriesProducto { get; set; }
        public DbSet<LotesProducto> LotesProducto { get; set; }

        public DbSet<CierresCaja> CierresCaja { get; set; }
        public DbSet<ConfiguracionSAR> ConfiguracionSAR { get; set; }
        public DbSet<FacturasEmitidas> FacturasEmitidas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Perfiles>().HasQueryFilter(e => e.Activo);
            modelBuilder.Entity<Usuarios>().HasQueryFilter(e => e.Activo);
            modelBuilder.Entity<Permisos>().HasQueryFilter(e => e.Activo);
            modelBuilder.Entity<Empresas>().HasQueryFilter(e => e.Activo);
            modelBuilder.Entity<Sucursales>().HasQueryFilter(e => e.Activo);
            modelBuilder.Entity<Producto>().HasQueryFilter(e => e.Activo);
            modelBuilder.Entity<Cliente>().HasQueryFilter(e => e.Activo);
            modelBuilder.Entity<Proveedores>().HasQueryFilter(e => e.Activo);
            modelBuilder.Entity<MetodosPago>().HasQueryFilter(e => e.Activo);
            modelBuilder.Entity<EstadosVenta>().HasQueryFilter(e => e.Activo);
            modelBuilder.Entity<Venta>().HasQueryFilter(e => e.Activo);
            modelBuilder.Entity<EstadosCompra>().HasQueryFilter(e => e.Activo);
            modelBuilder.Entity<Compra>().HasQueryFilter(e => e.Activo);
            modelBuilder.Entity<SeriesProducto>().HasQueryFilter(e => e.Activo);
            modelBuilder.Entity<LotesProducto>().HasQueryFilter(e => e.Activo);
            modelBuilder.Entity<CierresCaja>().HasQueryFilter(e => e.Activo);
            modelBuilder.Entity<ConfiguracionSAR>().HasQueryFilter(e => e.Activo);
            modelBuilder.Entity<FacturasEmitidas>().HasQueryFilter(e => e.Activo);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(WebWaveDbContext).Assembly);
        }
    }
}