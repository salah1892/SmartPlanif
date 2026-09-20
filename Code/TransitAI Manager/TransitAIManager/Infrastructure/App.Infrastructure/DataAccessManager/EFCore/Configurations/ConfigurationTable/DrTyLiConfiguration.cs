using Core.Domain.Entities.Metier;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.DataAccessManager.EFCore.Configurations.ConfigurationTable
{
    public class DrTyLiConfiguration: IEntityTypeConfiguration<DrTyLi>
    {
        public void Configure(EntityTypeBuilder<DrTyLi> builder)
        {
            builder.HasKey(e => new { e.Dectyli }); // Clé composite
        }
    }
}