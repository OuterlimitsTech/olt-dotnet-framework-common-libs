using System.Data.Entity.Infrastructure;

namespace OLT.Core
{
    public interface IOltDeletingRecord
    {
        void DeletingRecord(IOltDbContext db, DbEntityEntry entityEntry);
    }
}