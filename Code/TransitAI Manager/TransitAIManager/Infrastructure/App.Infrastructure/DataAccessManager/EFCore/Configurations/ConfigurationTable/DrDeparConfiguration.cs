using Core.Domain.Entities.Metier;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.DataAccessManager.EFCore.Configurations.ConfigurationTable
{
    public class DrDeparConfiguration : IEntityTypeConfiguration<DrDepar>
    {
        public void Configure(EntityTypeBuilder<DrDepar> builder)
        {
            builder.ToTable("DrDepar");

            // 1. Configure relationship with DrAgent
            builder.HasOne(d => d.drAgent)         // Navigation property
                   .WithMany()                     // Inverse navigation (if DrAgent has a list of DrDepar, add it here)
                   .HasForeignKey(d => d.DECAGEN)  // Explicitly map the FK to your existing column
                   .OnDelete(DeleteBehavior.NoAction);

            // 2. Configure relationship with DrLigne
            builder.HasOne(d => d.drLigne)
                   .WithMany()
                   .HasForeignKey(d => d.DENUMLI)  // Explicitly map the FK
                   .OnDelete(DeleteBehavior.NoAction);

            // 3. Configure relationship with DrVehic
            builder.HasOne(d => d.drVehic)
                   .WithMany()
                   .HasForeignKey(d => d.DECODVH)  // Explicitly map the FK
                   .OnDelete(DeleteBehavior.NoAction);

        }
    }
}