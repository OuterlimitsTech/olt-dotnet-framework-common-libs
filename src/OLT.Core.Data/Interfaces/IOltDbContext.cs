using System.Collections.Generic;
using System.Data.Entity;

namespace OLT.Core
{
    public interface IOltDbContext : IOltDbContext<Database>
    {
        void BulkCopy<TEntity>(IList<TEntity> entityList) where TEntity : class, IOltEntity;
        DbSet<TEntity> Set<TEntity>() where TEntity : class;
        //IEnumerable<DbEntityValidationResult> GetValidationErrors();
    }
}