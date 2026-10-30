using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class FacturasEmitidasMap : IEntityTypeConfiguration<FacturasEmitidas>
    {
        public void Configure(EntityTypeBuilder<FacturasEmitidas> builder)
        {
            builder.ToTable("FacturasEmitidas", "dbo");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("Id").HasColumnType("INT").IsRequired().ValueGeneratedOnAdd();
            builder.Property(x => x.VentaId).HasColumnName("VentaId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.SucursalId).HasColumnName("SucursalId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.NumeroFactura).HasColumnName("NumeroFactura").HasColumnType("VARCHAR(30)").IsRequired();
            builder.Property(x => x.CAI).HasColumnName("CAI").HasColumnType("VARCHAR(50)").IsRequired();
            builder.Property(x => x.FechaEmision).HasColumnName("FechaEmision").HasColumnType("TIMESTAMP WITHOUT TIME ZONE").IsRequired();
            builder.Property(x => x.RTNCliente).HasColumnName("RTNCliente").HasColumnType("VARCHAR(20)");
            builder.Property(x => x.NombreCliente).HasColumnName("NombreCliente").HasColumnType("VARCHAR(200)").IsRequired();
            builder.Property(x => x.MontoExento).HasColumnName("MontoExento").HasColumnType("NUMERIC(18,2)").IsRequired();
            builder.Property(x => x.MontoGravado15).HasColumnName("MontoGravado15").HasColumnType("NUMERIC(18,2)").IsRequired();
            builder.Property(x => x.MontoGravado18).HasColumnName("MontoGravado18").HasColumnType("NUMERIC(18,2)").IsRequired();
            builder.Property(x => x.ISV15).HasColumnName("ISV15").HasColumnType("NUMERIC(18,2)").IsRequired();
            builder.Property(x => x.ISV18).HasColumnName("ISV18").HasColumnType("NUMERIC(18,2)").IsRequired();
            builder.Property(x => x.Total).HasColumnName("Total").HasColumnType("NUMERIC(18,2)").IsRequired();
            builder.Property(x => x.Activo).HasColumnName("Activo").HasColumnType("BOOLEAN").IsRequired();

            builder.HasOne(x => x.Venta)
                   .WithMany()
                   .HasForeignKey(x => x.VentaId);

            builder.HasOne(x => x.Sucursal)
                   .WithMany()
                   .HasForeignKey(x => x.SucursalId);
        }
    }
}
