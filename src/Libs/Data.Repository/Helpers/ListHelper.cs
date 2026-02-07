using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repository.Helpers
{
    public static class ListHelper
    {
        public static void Replace<T>(this List<T> source, List<T> target)
        {
            if (source == null || target == null)
                return;
            
            source.Clear();
            source.AddRange(target);
        }

        public static void AllDeleted<T>(this DbContext source, List<T> list)
        {
            list.ForEach(_ => source.Entry(_).State = EntityState.Deleted);
        }
    }
}
