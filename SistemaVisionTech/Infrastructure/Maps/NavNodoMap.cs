using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class NavNodoMap : IEntityTypeConfiguration<NavNodo>
    {
        public void Configure(EntityTypeBuilder<NavNodo> builder)
        {
            builder.ToTable("NavNodos", "dbo");
            builder.HasKey(x => x.NodoId);
            builder.Property(x => x.Nombre).HasColumnName("Nombre").HasColumnType("NVARCHAR(100)").IsRequired();
            builder.Property(x => x.Tipo).HasColumnName("Tipo").HasColumnType("NVARCHAR(10)").IsRequired();
            builder.Property(x => x.PadreId).HasColumnName("PadreId");
            builder.Property(x => x.Orden).HasColumnName("Orden").IsRequired();
            builder.Property(x => x.Icono).HasColumnName("Icono").HasColumnType("NVARCHAR(100)");
            builder.Property(x => x.Controller).HasColumnName("Controller").HasColumnType("NVARCHAR(50)");
            builder.Property(x => x.Action).HasColumnName("Action").HasColumnType("NVARCHAR(50)");
            builder.Property(x => x.Visible).HasColumnName("Visible").IsRequired();
            builder.Property(x => x.Activo).HasColumnName("Activo").IsRequired();

            builder.HasOne(x => x.Padre)
                .WithMany(x => x.Hijos)
                .HasForeignKey(x => x.PadreId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
