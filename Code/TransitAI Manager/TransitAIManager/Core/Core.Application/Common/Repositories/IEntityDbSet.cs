using Core.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Common.Repositories
{
    public interface IEntityDbSet
    {
        public DbSet<Token> Token { get; set; }
    }
}