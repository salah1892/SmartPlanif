using Core.Domain.Entities.Metier;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace App.Infrastructure.DataAccessManager.EFCore.Configurations.ConfigurationTable
{
    public class DrVehiculeConfiguration : IEntityTypeConfiguration<DrVehic>
    {
    
        public void Configure(EntityTypeBuilder<DrVehic> builder)
        {
            builder.ToTable("DrVehic");
            builder.HasKey(d => d.Decodvh);
            // Configuration de la relation
            builder.HasOne(d => d.Categorie)
                   .WithMany()                    // ou WithMany(c => c.Vehicules) si tu ajoutes la collection
                   .HasForeignKey(d => d.Decatvh) // Utilise explicitement la propriété existante
                   .HasPrincipalKey(c => c.Id)
                   .OnDelete(DeleteBehavior.Restrict); // ou NoAction selon ton besoin
        }
    }
}
