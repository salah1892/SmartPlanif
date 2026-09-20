using Core.Domain.Entities.Metier;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace App.Infrastructure.DataAccessManager.EFCore.Configurations.ConfigurationTable
{
    public class DrCatVeConfiguration : IEntityTypeConfiguration<DrCatVe>
    {
        public void Configure(EntityTypeBuilder<DrCatVe> builder)
        {
            builder.ToTable("drcatve");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                   .HasColumnName("DECATVH")
                   .ValueGeneratedNever();

            builder.Property(c => c.Decateg)
                   .HasColumnName("DECATEG");

            builder.Property(c => c.Deacate)
                   .HasColumnName("DEACATE");

            builder.Property(c => c.Capacity)
                   .HasColumnName("DENBPLC");
        }
    }
}
