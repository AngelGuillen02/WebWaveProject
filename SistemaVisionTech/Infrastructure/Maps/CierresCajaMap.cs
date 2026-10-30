using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class CierresCajaMap : IEntityTypeConfiguration<CierresCaja>
    {
        public void Configure(EntityTypeBuilder<CierresCaja> builder)
        {
            builder.ToTable("CierresCaja", "dbo");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("Id").HasColumnType("INT").IsRequired().ValueGeneratedOnAdd();
            builder.Property(x => x.SucursalId).HasColumnName("SucursalId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.UsuarioId).HasColumnName("UsuarioId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.FechaApertura).HasColumnName("FechaApertura").HasColumnType("TIMESTAMP WITHOUT TIME ZONE").IsRequired();
            builder.Property(x => x.FechaCierre).HasColumnName("FechaCierre").HasColumnType("TIMESTAMP WITHOUT TIME ZONE");
            builder.Property(x => x.MontoApertura).HasColumnName("MontoApertura").HasColumnType("NUMERIC(18,2)").IsRequired();
            builder.Property(x => x.MontoEfectivoFinal).HasColumnName("MontoEfectivoFinal").HasColumnType("NUMERIC(18,2)");
            builder.Property(x => x.TotalVentasEfectivo).HasColumnName("TotalVentasEfectivo").HasColumnType("NUMERIC(18,2)").IsRequired();
            builder.Property(x => x.TotalVentasTarjeta).HasColumnName("TotalVentasTarjeta").HasColumnType("NUMERIC(18,2)").IsRequired();
            builder.Property(x => x.TotalVentasOtros).HasColumnName("TotalVentasOtros").HasColumnType("NUMERIC(18,2)").IsRequired();
            builder.Property(x => x.Diferencia).HasColumnName("Diferencia").HasColumnType("NUMERIC(18,2)");
            builder.Property(x => x.Estado).HasColumnName("Estado").HasColumnType("VARCHAR(20)").IsRequired();
            builder.Property(x => x.Activo).HasColumnName("Activo").HasColumnType("BOOLEAN").IsRequired();

            builder.HasOne(x => x.Sucursal)
                   .WithMany()
                   .HasForeignKey(x => x.SucursalId);

            builder.HasOne(x => x.Usuario)
                   .WithMany()
                   .HasForeignKey(x => x.UsuarioId);
        }
    }
}
