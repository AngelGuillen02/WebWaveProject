using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class PerfilesPermisosMap : IEntityTypeConfiguration<PerfilesPermisos>
    {
        public void Configure(EntityTypeBuilder<PerfilesPermisos> builder)
        {
            builder.ToTable("PerfilesPermisos", "dbo");

            builder.HasKey(x => x.PerfilPermisoId);

            builder.Property(x => x.PerfilPermisoId).HasColumnName("PerfilPermisoId").HasColumnType("INT").IsRequired();

            builder.Property(x => x.PerfilId).HasColumnName("PerfilId").HasColumnType("INT").IsRequired();

            builder.Property(x => x.PermisoId).HasColumnName("PermisoId").HasColumnType("INT").IsRequired();

            builder.HasOne(x => x.Perfil).WithMany(x => x.Permisos).HasForeignKey(x => x.PerfilId);

            builder.HasOne(x => x.Permiso).WithMany(x => x.Perfiles).HasForeignKey(x => x.PermisoId);

            builder.HasIndex(x => new { x.PerfilId, x.PermisoId }).IsUnique();
        }
    }
}