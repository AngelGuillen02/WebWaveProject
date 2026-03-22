using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class ProductosMap : IEntityTypeConfiguration<Productos>
    {
        public void Configure(EntityTypeBuilder<Productos> builder)
        {
            builder.ToTable("Productos", "dbo");
            builder.HasKey(x => x.ProductoId);
            builder.Property(x => x.ProductoId).HasColumnName("ProductoId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.Nombre).HasColumnName("Nombre").HasColumnType("VARCHAR(255)").IsRequired();
            builder.Property(x => x.Descripcion).HasColumnName("Descripcion").HasColumnType("VARCHAR(500)");
            builder.Property(x => x.Precio).HasColumnName("Precio").HasColumnType("DECIMAL(18,2)").IsRequired();
        }
    }
}
