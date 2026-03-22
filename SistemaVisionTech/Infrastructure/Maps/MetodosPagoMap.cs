using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class MetodosPagoMap : IEntityTypeConfiguration<MetodosPago>
    {
        public void Configure(EntityTypeBuilder<MetodosPago> builder)
        {
            builder.ToTable("MetodosPago", "dbo");
            builder.HasKey(x => x.MetodoPagoId);
            builder.Property(x => x.MetodoPagoId).HasColumnName("MetodoPagoId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.Nombre).HasColumnName("Nombre").HasColumnType("VARCHAR(255)").IsRequired();
        }
    }
}
