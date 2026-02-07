using System.Reflection;
using Data.Repository;

namespace SpaceDb.Data.SpaceDb.DatabaseContext
{
    public static class SpaceDbContextExtension
    {
        public static bool AllMigrationsApplied(this SpaceDbContext context)
        {
            return context.AllMigrationsAppliedCore();
        }

        public static void EnsureSeeded(this SpaceDbContext context)
        {
            context.EnsureSeededCore(_ =>
                {
                    var dbAssembly = Assembly.GetExecutingAssembly();
                    context.AddSeedFromJson(context.Roles, dbAssembly, "Role", _ => _.Id, null, null, "Data.SpaceDb");
                    context.SaveChanges();
                });
        }
    }
}
