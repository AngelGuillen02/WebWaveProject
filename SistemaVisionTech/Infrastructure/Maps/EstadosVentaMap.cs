using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class EstadosVentaMap : IEntityTypeConfiguration<EstadosVenta>
    {
        public void Configure(EntityTypeBuilder<EstadosVenta> builder)
        {
            builder.ToTable("EstadosVenta", "dbo");
            builder.HasKey(x => x.EstadoId);
            builder.Property(x => x.EstadoId).HasColumnName("EstadoId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.Nombre).HasColumnName("Nombre").HasColumnType("VARCHAR(255)").IsRequired();
        }
    }
}
