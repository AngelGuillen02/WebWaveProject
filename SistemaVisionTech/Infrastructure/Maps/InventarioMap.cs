using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class InventarioMap : IEntityTypeConfiguration<Inventarios>
    {
        public void Configure(EntityTypeBuilder<Inventarios> builder)
        {
            builder.ToTable("Inventario", "dbo");
            builder.HasKey(x => x.InventarioId);
            builder.Property(x => x.InventarioId).HasColumnName("InventarioId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.ProductoId).HasColumnName("ProductoId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.Cantidad).HasColumnName("Cantidad").HasColumnType("INT").IsRequired();
            builder.Property(x => x.FechaIngreso).HasColumnName("FechaIngreso").IsRequired();

            builder.HasOne(x => x.Producto)
                .WithMany(p => p.Inventarios)
                .HasForeignKey(x => x.ProductoId)
                .IsRequired(false);
        }
    }
}
