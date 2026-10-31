using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class VentasMap : IEntityTypeConfiguration<Venta>
    {
        public void Configure(EntityTypeBuilder<Venta> builder)
        {
            builder.ToTable("Ventas", "dbo");
            builder.HasKey(x => x.VentaId);
            builder.Property(x => x.VentaId).HasColumnName("VentaId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.ClienteId).HasColumnName("ClienteId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.FechaVenta).HasColumnName("FechaVenta").IsRequired();
            builder.Property(x => x.Total).HasColumnName("Total").HasColumnType("DECIMAL(18,2)").IsRequired();
            builder.Property(x => x.EstadoVentaId).HasColumnName("EstadoVentaId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.EsCotizacion).HasColumnName("EsCotizacion").HasColumnType("BOOLEAN").HasDefaultValue(false).IsRequired();
            builder.Property(x => x.NumeroCotizacion).HasColumnName("NumeroCotizacion").HasColumnType("VARCHAR(50)");
            builder.Ignore(x => x.Rtn);
        }
    }
}
