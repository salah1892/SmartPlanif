using Core.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.DataAccessManager.EFCore.Common
{
    public abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : BaseEntity
    {
        public virtual void Configure(EntityTypeBuilder<T> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasMaxLength(Constants.IdConsts.MaxLength)
                .IsRequired(true);
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .IsRequired(true);
            builder.Property(e => e.CreatedAtUtc)
                .IsRequired(false);
            builder.Property(e => e.CreatedById)
                .HasMaxLength(Constants.UserIdConsts.MaxLength)
                .IsRequired(false);
            builder.Property(e => e.UpdatedAtUtc)
                .IsRequired(false);
            builder.Property(e => e.UpdatedById)
                .HasMaxLength(Constants.UserIdConsts.MaxLength)
                .IsRequired(false);
        }
    }
}