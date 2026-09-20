using Core.Application.Common.Repositories;

namespace Core.Application.Common.CQS.Queries
{
    public interface IQueryContext : IEntityDbSet
    {
        IQueryable<T> Set<T>() where T : class;
    }
}