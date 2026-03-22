using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class PagosCompraMap : IEntityTypeConfiguration<PagosCompra>
    {
        public void Configure(EntityTypeBuilder<PagosCompra> builder)
        {
            builder.ToTable("PagosCompra", "dbo");
            builder.HasKey(x => x.PagoCompraId);
            builder.Property(x => x.PagoCompraId).HasColumnName("PagoCompraId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.CompraId).HasColumnName("CompraId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.MetodoPagoId).HasColumnName("MetodoPagoId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.Monto).HasColumnName("Monto").HasColumnType("DECIMAL(18,2)").IsRequired();
            builder.Property(x => x.FechaPago).HasColumnName("FechaPago").HasColumnType("DATETIME").IsRequired();
        }
    }
}
