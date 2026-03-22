using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class ProveedoresMap : IEntityTypeConfiguration<Proveedores>
    {
        public void Configure(EntityTypeBuilder<Proveedores> builder)
        {
            builder.ToTable("Proveedores", "dbo");
            builder.HasKey(x => x.ProveedorId);
            builder.Property(x => x.ProveedorId).HasColumnName("ProveedorId").HasColumnType("INT").IsRequired();
            builder.Property(x => x.Nombre).HasColumnName("Nombre").HasColumnType("VARCHAR(255)").IsRequired();
            builder.Property(x => x.Direccion).HasColumnName("Direccion").HasColumnType("VARCHAR(255)");
            builder.Property(x => x.Telefono).HasColumnName("Telefono").HasColumnType("VARCHAR(50)");
            builder.Property(x => x.Email).HasColumnName("Email").HasColumnType("VARCHAR(255)");
        }
    }
}
