using Core.Domain.Entities.Metier;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;

namespace App.Infrastructure.DataAccessManager.EFCore.Configurations.ConfigurationTable
{
    public class DrLigneConfiguration : IEntityTypeConfiguration<DrLigne>
    {
        public void Configure(EntityTypeBuilder<DrLigne> builder)
        {
            builder.HasKey(e => new { e.Denumli }); // Clé composite
            builder
                .HasOne(l => l.Categorie)
            .WithMany()
            .HasForeignKey(l => l.Dectyli);
        }
    }
}