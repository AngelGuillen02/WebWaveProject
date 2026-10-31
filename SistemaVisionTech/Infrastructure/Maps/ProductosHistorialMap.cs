using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class ProductosHistorialMap : IEntityTypeConfiguration<ProductosHistorial>
    {
        public void Configure(EntityTypeBuilder<ProductosHistorial> builder)
        {
            builder.ToTable("ProductosHistorial", "dbo");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("Id").HasColumnType("INT").IsRequired();
            builder.Property(x => x.ProductoId).HasColumnName("ProductoId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.Usuario).HasColumnName("Usuario").HasColumnType("VARCHAR(100)").IsRequired();
            builder.Property(x => x.FechaModificacion).HasColumnName("FechaModificacion").IsRequired();
            builder.Property(x => x.Accion).HasColumnName("Accion").HasColumnType("VARCHAR(50)").IsRequired();
            builder.Property(x => x.DetalleCambios).HasColumnName("DetalleCambios").HasColumnType("TEXT").IsRequired();
            builder.Property(x => x.PrecioAnterior).HasColumnName("PrecioAnterior").HasColumnType("DECIMAL(18,2)");
            builder.Property(x => x.PrecioNuevo).HasColumnName("PrecioNuevo").HasColumnType("DECIMAL(18,2)").IsRequired();
            builder.Property(x => x.Activo).HasColumnName("Activo").HasColumnType("BOOLEAN").IsRequired();

            builder.HasOne(x => x.Producto)
                   .WithMany()
                   .HasForeignKey(x => x.ProductoId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
