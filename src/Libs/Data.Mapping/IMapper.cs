using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Mapping
{
    public interface IMapper
    {
        public TDest Map<TSource, TDest, TMapOptions>(TSource source, TMapOptions options = null)
            where TMapOptions : MapOptions;
        public IEnumerable<TDest> Map<TSource, TDest, TMapOptions>(IEnumerable<TSource> source, TMapOptions options = null)
            where TMapOptions : MapOptions;
    }
}
