using System.Data.Entity.Infrastructure;

namespace OLT.Core
{
    public interface IOltUpdatingRecord
    {
        void UpdatingRecord(IOltDbContext db, DbEntityEntry entityEntry);
    }
}