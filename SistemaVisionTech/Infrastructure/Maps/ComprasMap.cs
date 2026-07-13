using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class ComprasMap : IEntityTypeConfiguration<Compra>
    {
        public void Configure(EntityTypeBuilder<Compra> builder)
        {
            builder.ToTable("Compras", "dbo");
            builder.HasKey(x => x.CompraId);
            builder.Property(x => x.CompraId).HasColumnName("CompraId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.ProveedorId).HasColumnName("ProveedorId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.FechaCompra).HasColumnName("FechaCompra").HasColumnType("DATETIME").IsRequired();
            builder.Property(x => x.Total).HasColumnName("Total").HasColumnType("DECIMAL(18,2)").IsRequired();
            builder.Property(x => x.EstadoCompraId).HasColumnName("EstadoCompraId").HasColumnType("INT").IsRequired();
            builder.HasOne(c => c.Proveedor).WithMany(p => p.Compras).HasForeignKey(c => c.ProveedorId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(c => c.EstadoCompra).WithMany(e => e.Compras).HasForeignKey(c => c.EstadoCompraId);
            builder.HasMany(c => c.Detalles).WithOne(d => d.Compra).HasForeignKey(d => d.CompraId);
            builder.HasMany(c => c.Pagos).WithOne(p => p.Compra).HasForeignKey(p => p.CompraId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
