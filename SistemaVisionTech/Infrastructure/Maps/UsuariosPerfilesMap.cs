using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class UsuariosPerfilesMap : IEntityTypeConfiguration<UsuariosPerfiles>
    {
        public void Configure(EntityTypeBuilder <UsuariosPerfiles> builder1)
        {
            builder1.ToTable("UsuariosPerfiles", "dbo");
            builder1.HasKey(x => x.IdUsuariosPerfiles);
            builder1.Property(x => x.IdUsuariosPerfiles).HasColumnName("IdUsuariosPerfiles").HasColumnType("INT").IsRequired();
            builder1.Property(x => x.UsuarioId).HasColumnName("UsuarioId").HasColumnType("INT").IsRequired();
            builder1.Property(x => x.PerfilId).HasColumnName("PerfilId").HasColumnType("INT").IsRequired();
        }
    }
}
