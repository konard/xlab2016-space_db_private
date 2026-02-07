using Data.Mapping;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Data.Repository.Helpers
{
    public static class RepositoryHelper
    {
        public static IQueryable<T> ByQueryPaging<T>(this IQueryable<T> source, Paging paging)
        {
            if (paging == null)
                return source;

            if (paging.Skip.HasValue)
                source = source.Skip(paging.Skip.Value);
            if (paging.Take.HasValue)
                source = source.Take(paging.Take.Value);

            return source;
        }

        public static void Map<T>(this DbContext db, MapListPair<T> pair)
        {
            if (pair.Source == null || pair.Target == null)
                return;
            db.AllDeleted(pair.Target);
            pair.Target.Replace(pair.Source);
        }
    }
}
