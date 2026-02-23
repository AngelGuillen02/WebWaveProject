using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class PerfilesMap : IEntityTypeConfiguration<Perfiles>
    {
        public void Configure(EntityTypeBuilder<Perfiles> builder)
        {
            builder.ToTable("Perfiles", "dbo");
            builder.HasKey(x => x.PerfilId);
            builder.Property(x => x.PerfilId).HasColumnName("PerfilId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.Nombre).HasColumnName("Nombre").HasColumnType("VARCHAR(255)").IsRequired();
        }
    }
}