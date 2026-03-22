using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class ComprasDetallesMap : IEntityTypeConfiguration<ComprasDetalles>
    {
        public void Configure(EntityTypeBuilder<ComprasDetalles> builder)
        {
            builder.ToTable("ComprasDetalles", "dbo");
            builder.HasKey(x => x.CompraDetalleId);
            builder.Property(x => x.CompraDetalleId).HasColumnName("CompraDetalleId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.CompraId).HasColumnName("CompraId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.ProductoId).HasColumnName("ProductoId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.Cantidad).HasColumnName("Cantidad").HasColumnType("INT").IsRequired();
            builder.Property(x => x.PrecioUnitario).HasColumnName("PrecioUnitario").HasColumnType("DECIMAL(18,2)").IsRequired();
            builder.Property(x => x.Total).HasColumnName("Total").HasColumnType("DECIMAL(18,2)").IsRequired();
        }
    }
}
