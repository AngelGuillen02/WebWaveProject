using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class AuditoriaMap : IEntityTypeConfiguration<Auditoria>
    {
        public void Configure(EntityTypeBuilder<Auditoria> builder)
        {
            builder.ToTable("Auditoria", "dbo");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("Id").IsRequired();
            builder.Property(x => x.Tipo).HasColumnName("Tipo").HasMaxLength(50).IsRequired();
            builder.Property(x => x.EntidadId).HasColumnName("EntidadId").IsRequired();
            builder.Property(x => x.Referencia).HasColumnName("Referencia").HasMaxLength(200).IsRequired();
            builder.Property(x => x.Usuario).HasColumnName("Usuario").HasMaxLength(100).IsRequired();
            builder.Property(x => x.Fecha).HasColumnName("Fecha").IsRequired();
            builder.Property(x => x.Accion).HasColumnName("Accion").HasMaxLength(50).IsRequired();
            builder.Property(x => x.Detalle).HasColumnName("Detalle").IsRequired();
            builder.Property(x => x.ValorAnterior).HasColumnName("ValorAnterior").HasMaxLength(200);
            builder.Property(x => x.ValorNuevo).HasColumnName("ValorNuevo").HasMaxLength(200);
            builder.Property(x => x.Motivo).HasColumnName("Motivo").HasMaxLength(255);
            builder.Property(x => x.Activo).HasColumnName("Activo").IsRequired();
        }
    }
}
