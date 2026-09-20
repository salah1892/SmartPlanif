using Core.Domain.Entities.Metier;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.DataAccessManager.EFCore.Configurations.ConfigurationTable
{
    public class StopTimeConfiguration: IEntityTypeConfiguration<StopTime>
    {
        public void Configure(EntityTypeBuilder<StopTime> builder)
        {
            builder.HasKey(e => new { e.Dedated, e.TripId, e.StopSequence });
        }
    }
}