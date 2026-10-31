using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class CotizacionesHistorialMap : IEntityTypeConfiguration<CotizacionesHistorial>
    {
        public void Configure(EntityTypeBuilder<CotizacionesHistorial> builder)
        {
            builder.ToTable("CotizacionesHistorial", "dbo");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("Id").HasColumnType("INT").IsRequired();
            builder.Property(x => x.VentaId).HasColumnName("VentaId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.NumeroCotizacion).HasColumnName("NumeroCotizacion").HasColumnType("VARCHAR(50)");
            builder.Property(x => x.Usuario).HasColumnName("Usuario").HasColumnType("VARCHAR(100)").IsRequired();
            builder.Property(x => x.FechaModificacion).HasColumnName("FechaModificacion").IsRequired();
            builder.Property(x => x.Accion).HasColumnName("Accion").HasColumnType("VARCHAR(50)").IsRequired();
            builder.Property(x => x.DetalleCambios).HasColumnName("DetalleCambios").HasColumnType("TEXT").IsRequired();
            builder.Property(x => x.TotalAnterior).HasColumnName("TotalAnterior").HasColumnType("DECIMAL(18,2)");
            builder.Property(x => x.TotalNuevo).HasColumnName("TotalNuevo").HasColumnType("DECIMAL(18,2)").IsRequired();
            builder.Property(x => x.Motivo).HasColumnName("Motivo").HasColumnType("VARCHAR(255)");
            builder.Property(x => x.Activo).HasColumnName("Activo").HasColumnType("BOOLEAN").IsRequired();

            builder.HasOne(x => x.Venta)
                   .WithMany()
                   .HasForeignKey(x => x.VentaId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
