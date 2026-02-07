using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Mapping
{
    public abstract class MapDefinitionBase<TSource, TDest, TMapOptions, TMapContext> : IMapDefinition
        where TMapOptions : MapOptions
        where TMapContext : MapContextBase
    {
        public Type SourceType { get { return typeof(TSource); } }
        public Type DestinationType { get { return typeof(TDest); } }

        public TMapContext Context { get; set; }

        public MapDefinitionBase(TMapContext context)
        {
            Context = context;
        }

        public abstract TDest Map(TSource source, TMapOptions options = default);
        public virtual IEnumerable<TDest> Map(IEnumerable<TSource> source, TMapOptions options = default)
        {
            foreach (var sourceItem in source)
                yield return Map(sourceItem, options);
        }

        public abstract TSource ReverseMap(TDest source, TMapOptions options = default);
        public virtual IEnumerable<TSource> ReverseMap(IEnumerable<TDest> source, TMapOptions options = default)
        {
            foreach (var sourceItem in source)
                yield return ReverseMap(sourceItem, options);
        }
    }
}
