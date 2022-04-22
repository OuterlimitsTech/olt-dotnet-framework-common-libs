using System.Data.Entity;

namespace OLT.Core
{
    public interface IOltSeedDataHelper<out TContext>
        where TContext : DbContext
    {
        TContext Context { get; }
        void RunSeed();
    }
}