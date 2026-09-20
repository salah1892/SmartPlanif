using Core.Domain.Entities.Metier;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.DataAccessManager.EFCore.Configurations.ConfigurationTable
{
    public class DrAgentConfiguration: IEntityTypeConfiguration<DrAgent>
    {
        public void Configure(EntityTypeBuilder<DrAgent> builder)
        {
            builder.ToTable("dragent");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id)
                .HasColumnName("DECAGEN")
                .ValueGeneratedNever();
        
            // builder.Property(t => t.Id).HasColumnName("DECAGEN");
            builder.Property(t => t.FullNameAr).HasColumnName("DENAGEA");
            builder.Property(t => t.FullNameFr).HasColumnName("DENAGEN");
        
            builder.Property(e => e.QualificationCode)
                .HasColumnName("DECQUAL")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.JournalCode)
                .HasColumnName("DECJOUR")
                .IsRequired();

            builder.Property(e => e.CenterId)
                .HasColumnName("DECCENT")
                .IsRequired();

            builder.Property(e => e.DelegationId)
                .HasColumnName("DECDELEG")
                .IsRequired();
            // Ajout de la clé étrangère
            builder.HasOne<DrDeleg>()
                .WithMany(d => d.Agents)
                .HasForeignKey(e => e.DelegationId);
        
            builder.Property(e => e.PdaNumber)
                .HasColumnName("PDA"); // Nullable par défaut
            // DENAGEN varchar(50) 
            // DENAGEA varchar(100) 
            // DECQUAL varchar(50) 
            // DECJOUR int 
            //     DECCENT int 
            //     DECDELEG int 
            //     PDA
        }
    }
}