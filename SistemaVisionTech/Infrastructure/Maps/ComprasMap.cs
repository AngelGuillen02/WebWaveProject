using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class ComprasMap : IEntityTypeConfiguration<Compras>
    {
        public void Configure(EntityTypeBuilder<Compras> builder)
        {
            builder.ToTable("Compras", "dbo");
            builder.HasKey(x => x.CompraId);
            builder.Property(x => x.CompraId).HasColumnName("CompraId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.ProveedorId).HasColumnName("ProveedorId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.FechaCompra).HasColumnName("FechaCompra").HasColumnType("DATETIME").IsRequired();
            builder.Property(x => x.Total).HasColumnName("Total").HasColumnType("DECIMAL(18,2)").IsRequired();
            builder.Property(x => x.EstadoCompraId).HasColumnName("EstadoCompraId").HasColumnType("INT").IsRequired();
        }
    }
}
