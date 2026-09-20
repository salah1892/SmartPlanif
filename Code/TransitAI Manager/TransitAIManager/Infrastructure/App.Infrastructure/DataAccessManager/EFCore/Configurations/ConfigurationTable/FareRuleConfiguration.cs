using Core.Domain.Entities.Metier;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.DataAccessManager.EFCore.Configurations.ConfigurationTable
{
    public class FareRuleConfiguration: IEntityTypeConfiguration<FareRule>
    {
        public void Configure(EntityTypeBuilder<FareRule> builder)
        {
            builder.HasKey(e => new { e.FareId, e.RouteId }); // Clé composite
        }
    }
}