using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class HistorialMovimientoInventarioMap : IEntityTypeConfiguration<HistorialMovimientoInventario>
    {
        public void Configure(EntityTypeBuilder<HistorialMovimientoInventario> builder)
        {
            builder.ToTable("HistorialMovimientoInventario", "dbo");
            builder.HasKey(x => x.MovimientoId);
            builder.Property(x => x.MovimientoId).HasColumnName("MovimientoId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.InventarioId).HasColumnName("InventarioId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.Cantidad).HasColumnName("Cantidad").HasColumnType("INT").IsRequired();
            builder.Property(x => x.TipoMovimiento).HasColumnName("TipoMovimiento").HasColumnType("VARCHAR(50)").IsRequired();
            builder.Property(x => x.FechaMovimiento).HasColumnName("FechaMovimiento").HasColumnType("DATETIME").IsRequired();
        }
    }
}
