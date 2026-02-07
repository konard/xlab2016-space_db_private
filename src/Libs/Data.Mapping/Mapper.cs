using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Mapping
{
    public class Mapper<TMapContext> : IMapper
        where TMapContext : MapContextBase, new()
    {
        private Dictionary<string, IMapDefinition> registry = new Dictionary<string, IMapDefinition>();

        public TMapContext MapContext { get; set; }

        public Mapper()
        {
            MapContext = new TMapContext { Mapper = this };
        }

        public void Register(IMapDefinition mappingDefinition)
        {
            var key = GetKey(mappingDefinition);

            if (registry.ContainsKey(key))
                return;
            registry.Add(key, mappingDefinition);
        }

        public void UnRegister(IMapDefinition mappingDefinition)
        {
            var key = GetKey(mappingDefinition);

            if (!registry.ContainsKey(key))
                return;
            registry.Remove(key);
        }

        public IMapDefinition Find<TSource, TDest>()
        {
            var key = GetKey<TSource, TDest>();
            if (!registry.ContainsKey(key))
                return null;
            return registry[key];
        }

        public IMapDefinition ReverseFind<TSource, TDest>()
        {
            var key = GetKey<TDest, TSource>();
            if (!registry.ContainsKey(key))
                return null;
            return registry[key];
        }

        public bool TryFind<TSource, TDest, TMapOptions>(out MapDefinitionBase<TSource, TDest, TMapOptions, TMapContext> mappingDefinition,
            out MapDefinitionBase<TDest, TSource, TMapOptions, TMapContext> reverseMappingDefinition)
            where TMapOptions : MapOptions
        {
            mappingDefinition = null;
            reverseMappingDefinition = null;

            var definition = Find<TSource, TDest>();

            if (definition != null)
            {
                mappingDefinition = definition as MapDefinitionBase<TSource, TDest, TMapOptions, TMapContext>;
                return mappingDefinition != null;
            }
            else
            {
                definition = ReverseFind<TSource, TDest>();
                if (definition == null)
                    return false;

                reverseMappingDefinition = definition as MapDefinitionBase<TDest, TSource, TMapOptions, TMapContext>;
                return reverseMappingDefinition != null;
            }
        }

        public TDest Map<TSource, TDest, TMapOptions>(TSource source, TMapOptions options = null) 
            where TMapOptions : MapOptions
        {
            MapDefinitionBase<TSource, TDest, TMapOptions, TMapContext> mappingDefinition;
            MapDefinitionBase<TDest, TSource, TMapOptions, TMapContext> reverseMappingDefinition;

            if (!TryFind(out mappingDefinition, out reverseMappingDefinition))
                throw new MapNotFoundException(typeof(TSource), typeof(TDest));

            if (mappingDefinition != null)
                return mappingDefinition.Map(source, options);
            
            return reverseMappingDefinition.ReverseMap(source);
        }

        public IEnumerable<TDest> Map<TSource, TDest, TMapOptions>(IEnumerable<TSource> source, TMapOptions options = null) where TMapOptions : MapOptions
        {
            MapDefinitionBase<TSource, TDest, TMapOptions, TMapContext> mappingDefinition;
            MapDefinitionBase<TDest, TSource, TMapOptions, TMapContext> reverseMappingDefinition;

            if (!TryFind(out mappingDefinition, out reverseMappingDefinition))
                throw new MapNotFoundException(typeof(TSource), typeof(TDest));

            if (mappingDefinition != null)
                return mappingDefinition.Map(source, options);

            return reverseMappingDefinition.ReverseMap(source);
        }

        private string GetKey(IMapDefinition mappingDefinition)
        {
            return mappingDefinition.SourceType.FullName + "=>" + mappingDefinition.DestinationType.FullName;
        }

        private string GetKey<TSource, TDest>()
        {
            return typeof(TSource).FullName + "=>" + typeof(TDest).FullName;
        }
    }
}
