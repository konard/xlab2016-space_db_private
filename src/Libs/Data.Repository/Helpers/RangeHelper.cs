using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository.Helpers
{
    public static class RangeHelper
    {
        public static List<T> SequenceWithoutGaps<T, TKey>(this List<T> source, Func<T, TKey> keySelector, Func<TKey, TKey, bool> compare, Func<TKey, T> createForGap)
        {
            for (var i = 0; i < source.Count - 1; i++)
            {
                var key1 = keySelector(source[i]);
                var key2 = keySelector(source[i + 1]);

                if (!compare(key1, key2))
                {
                    source.Insert(i + 1, createForGap(key1));
                }
            }

            return source;
        }
    }
}
