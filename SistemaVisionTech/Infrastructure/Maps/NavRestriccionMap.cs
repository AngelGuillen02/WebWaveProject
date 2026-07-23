using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Infrastructure.Maps
{
    public class NavRestriccionMap : IEntityTypeConfiguration<NavRestriccion>
    {
        public void Configure(EntityTypeBuilder<NavRestriccion> builder)
        {
            builder.ToTable("NavRestricciones", "dbo");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.NodoId).HasColumnName("NodoId").IsRequired();
            builder.Property(x => x.PerfilId).HasColumnName("PerfilId").IsRequired();

            builder.HasOne(x => x.Nodo)
                .WithMany(x => x.Restricciones)
                .HasForeignKey(x => x.NodoId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Perfil)
                .WithMany()
                .HasForeignKey(x => x.PerfilId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
