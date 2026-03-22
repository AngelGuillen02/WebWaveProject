using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class PagosVentaMap : IEntityTypeConfiguration<PagosVenta>
    {
        public void Configure(EntityTypeBuilder<PagosVenta> builder)
        {
            builder.ToTable("PagosVenta", "dbo");
            builder.HasKey(x => x.PagoVentaId);
            builder.Property(x => x.PagoVentaId).HasColumnName("PagoVentaId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.VentaId).HasColumnName("VentaId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.MetodoPagoId).HasColumnName("MetodoPagoId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.Monto).HasColumnName("Monto").HasColumnType("DECIMAL(18,2)").IsRequired();
            builder.Property(x => x.FechaPago).HasColumnName("FechaPago").HasColumnType("DATETIME").IsRequired();
        }
    }
}
