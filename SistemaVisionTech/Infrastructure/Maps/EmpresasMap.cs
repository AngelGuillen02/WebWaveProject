using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class EmpresasMap : IEntityTypeConfiguration<Empresas>
    {
        public void Configure(EntityTypeBuilder<Empresas> builder)
        {
            builder.ToTable("Empresas", "dbo");
            builder.HasKey(x => x.EmpresaId);
            builder.Property(x => x.EmpresaId).HasColumnName("EmpresaId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.Nombre).HasColumnName("Nombre").HasColumnType("VARCHAR(255)").IsRequired();
            builder.Property(x => x.Direccion).HasColumnName("Direccion").HasColumnType("VARCHAR(255)");
            builder.Property(x => x.Telefono).HasColumnName("Telefono").HasColumnType("VARCHAR(50)");
            builder.Property(x => x.Email).HasColumnName("Email").HasColumnType("VARCHAR(255)");
        }
    }
}
