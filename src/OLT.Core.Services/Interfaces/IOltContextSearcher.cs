using System.Data.Entity;
using System.Linq;

namespace OLT.Core
{
    public interface IOltContextSearcher<in TContext, TEntity> : IOltSearcher<TEntity>
        where TEntity : class, IOltEntity
        where TContext : DbContext, IOltDbContext
    {
        IQueryable<TEntity> BuildQueryable(TContext context, IQueryable<TEntity> queryable);
    }

}