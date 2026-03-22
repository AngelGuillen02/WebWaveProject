using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class InventarioMap : IEntityTypeConfiguration<Inventario>
    {
        public void Configure(EntityTypeBuilder<Inventario> builder)
        {
            builder.ToTable("Inventario", "dbo");
            builder.HasKey(x => x.InventarioId);
            builder.Property(x => x.InventarioId).HasColumnName("InventarioId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.ProductoId).HasColumnName("ProductoId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.Cantidad).HasColumnName("Cantidad").HasColumnType("INT").IsRequired();
            builder.Property(x => x.FechaIngreso).HasColumnName("FechaIngreso").HasColumnType("DATETIME").IsRequired();
        }
    }
}
