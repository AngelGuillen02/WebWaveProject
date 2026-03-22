using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class EstadosCompraMap : IEntityTypeConfiguration<EstadosCompra>
    {
        public void Configure(EntityTypeBuilder<EstadosCompra> builder)
        {
            builder.ToTable("EstadosCompra", "dbo");
            builder.HasKey(x => x.EstadoId);
            builder.Property(x => x.EstadoId).HasColumnName("EstadoId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.Nombre).HasColumnName("Nombre").HasColumnType("VARCHAR(255)").IsRequired();
        }
    }
}
