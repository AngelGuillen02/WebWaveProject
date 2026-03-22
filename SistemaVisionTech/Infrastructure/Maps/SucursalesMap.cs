using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class SucursalesMap : IEntityTypeConfiguration<Sucursales>
    {
        public void Configure(EntityTypeBuilder<Sucursales> builder)
        {
            builder.ToTable("Sucursales", "dbo");
            builder.HasKey(x => x.SucursalId);
            builder.Property(x => x.SucursalId).HasColumnName("SucursalId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.Nombre).HasColumnName("Nombre").HasColumnType("VARCHAR(255)").IsRequired();
            builder.Property(x => x.Direccion).HasColumnName("Direccion").HasColumnType("VARCHAR(255)");
            builder.Property(x => x.EmpresaId).HasColumnName("EmpresaId").HasColumnType("INT").IsRequired();
        }
    }
}
