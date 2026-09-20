using App.Infrastructure.DataAccessManager.EFCore.Common;
using Core.Domain.Common;
using Core.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.DataAccessManager.EFCore.Configurations.AspNetIdentity
{
    public class TokenConfiguration : BaseEntityConfiguration<Token>
    {
        public override void Configure(EntityTypeBuilder<Token> builder)
        {
            base.Configure(builder);

            builder.Property(e => e.UserId).HasMaxLength(Constants.UserIdConsts.MaxLength).IsRequired(false);
            builder.Property(e => e.RefreshToken).HasMaxLength(Constants.LengthConsts.M).IsRequired(false);
        }
    }
}