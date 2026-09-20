using Core.Application.Common.CQS.Queries;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.DataAccessManager.EFCore.Contexts
{
    public class QueryContext : DataContext, IQueryContext
    {
        public QueryContext(DbContextOptions<QueryContext> options)
            : base(options)
        {
        }

        public new IQueryable<T> Set<T>() where T : class
        {
            return base.Set<T>();
        }
    }
}