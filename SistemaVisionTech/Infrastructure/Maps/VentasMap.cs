using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class VentasMap : IEntityTypeConfiguration<Ventas>
    {
        public void Configure(EntityTypeBuilder<Ventas> builder)
        {
            builder.ToTable("Ventas", "dbo");
            builder.HasKey(x => x.VentaId);
            builder.Property(x => x.VentaId).HasColumnName("VentaId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.ClienteId).HasColumnName("ClienteId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.FechaVenta).HasColumnName("FechaVenta").HasColumnType("DATETIME").IsRequired();
            builder.Property(x => x.Total).HasColumnName("Total").HasColumnType("DECIMAL(18,2)").IsRequired();
            builder.Property(x => x.EstadoVentaId).HasColumnName("EstadoVentaId").HasColumnType("INT").IsRequired();
        }
    }
}
