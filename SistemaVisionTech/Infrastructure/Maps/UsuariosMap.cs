using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class UsuariosMap : IEntityTypeConfiguration<Usuarios>
    {
        public void Configure(EntityTypeBuilder<Usuarios> builder)
        {
            builder.ToTable("Usuarios", "dbo");

            builder.HasKey(x => x.UsuarioId);
            builder.Property(x => x.UsuarioId).HasColumnName("UsuarioId").HasColumnType("INT").IsRequired();

            builder.Property(x => x.Nombre).HasColumnName("Nombre").HasColumnType("VARCHAR(255)").IsRequired();

            builder.Property(x => x.Email).HasColumnName("Email").HasColumnType("VARCHAR(255)").IsRequired();

            builder.Property(x => x.Contraseña).HasColumnName("Contraseña").HasColumnType("VARCHAR(255)").IsRequired();

            builder.Property(x => x.PerfilId).HasColumnName("PerfilId").HasColumnType("INT").IsRequired();
        }
    }
}
