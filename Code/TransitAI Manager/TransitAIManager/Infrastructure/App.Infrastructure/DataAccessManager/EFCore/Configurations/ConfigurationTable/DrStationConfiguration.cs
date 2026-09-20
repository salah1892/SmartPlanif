using Core.Domain.Entities.Metier;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace App.Infrastructure.DataAccessManager.EFCore.Configurations.ConfigurationTable
{
    public class DrStationConfiguration : IEntityTypeConfiguration<DrStati>
    {
        public void Configure(EntityTypeBuilder<DrStati> builder)
        {
            builder.HasKey(e => new { e.DecStat }); // Clé composite
        }
    }

}
