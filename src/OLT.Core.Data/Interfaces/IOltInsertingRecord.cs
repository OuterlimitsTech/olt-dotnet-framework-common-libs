using System.Data.Entity.Infrastructure;

namespace OLT.Core
{
    public interface IOltInsertingRecord
    {
        void InsertingRecord(IOltDbContext db, DbEntityEntry entityEntry);
    }
}