using App.Infrastructure.DataAccessManager.EFCore.Common;
using Core.Domain.Entities.Identity;
using Core.Domain.Entities.Metier;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.DataAccessManager.EFCore.Configurations.ConfigurationTable
{
    public class DrItinConfiguration: IEntityTypeConfiguration<DrItin>
    {
        public  void Configure(EntityTypeBuilder<DrItin> builder)
        {
            builder.HasKey(e => new { e.Denumli, e.Denumlg, e.Decstat });
        }
    }
}