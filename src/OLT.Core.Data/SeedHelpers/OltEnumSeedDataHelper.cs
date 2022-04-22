using System.Data.Entity;

namespace OLT.Core
{
    public class OltEnumSeedDataHelper<TData, TEnum, TContext> : OltSeed<TContext>
        where TContext : DbContext
        where TData : class, IOltEntityCodeValueEnum, new()
        where TEnum : struct
    {
        public OltEnumSeedDataHelper(TContext context) : base(context)
        {

        }

        public override void RunSeed()
        {
            SeedFromEnum<TData, TEnum>();
            Context.SaveChanges();
        }
    }
}