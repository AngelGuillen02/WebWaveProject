using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class ConfiguracionSARMap : IEntityTypeConfiguration<ConfiguracionSAR>
    {
        public void Configure(EntityTypeBuilder<ConfiguracionSAR> builder)
        {
            builder.ToTable("ConfiguracionSAR", "dbo");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("Id").HasColumnType("INT").IsRequired().ValueGeneratedOnAdd();
            builder.Property(x => x.SucursalId).HasColumnName("SucursalId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.RTN).HasColumnName("RTN").HasColumnType("VARCHAR(20)").IsRequired();
            builder.Property(x => x.CAI).HasColumnName("CAI").HasColumnType("VARCHAR(50)").IsRequired();
            builder.Property(x => x.RangoDesde).HasColumnName("RangoDesde").HasColumnType("VARCHAR(20)").IsRequired();
            builder.Property(x => x.RangoHasta).HasColumnName("RangoHasta").HasColumnType("VARCHAR(20)").IsRequired();
            builder.Property(x => x.FechaLimiteEmision).HasColumnName("FechaLimiteEmision").HasColumnType("DATE").IsRequired();
            builder.Property(x => x.CorrelativoActual).HasColumnName("CorrelativoActual").HasColumnType("INT").IsRequired();
            builder.Property(x => x.Activo).HasColumnName("Activo").HasColumnType("BOOLEAN").IsRequired();

            builder.HasOne(x => x.Sucursal)
                   .WithMany()
                   .HasForeignKey(x => x.SucursalId);
        }
    }
}
