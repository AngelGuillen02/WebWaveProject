using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class VentasDetallesMap : IEntityTypeConfiguration<VentasDetalles>
    {
        public void Configure(EntityTypeBuilder<VentasDetalles> builder)
        {
            builder.ToTable("VentasDetalles", "dbo");
            builder.HasKey(x => x.VentaDetalleId);
            builder.Property(x => x.VentaDetalleId).HasColumnName("VentaDetalleId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.VentaId).HasColumnName("VentaId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.ProductoId).HasColumnName("ProductoId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.Cantidad).HasColumnName("Cantidad").HasColumnType("INT").IsRequired();
            builder.Property(x => x.Precio).HasColumnName("Precio").HasColumnType("DECIMAL(18,2)").IsRequired();
            builder.Property(x => x.Total).HasColumnName("Total").HasColumnType("DECIMAL(18,2)").IsRequired();
        }
    }
}
