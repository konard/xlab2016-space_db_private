using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Mapping
{
    public abstract class MapBase3<TSource, TDest, TMapOptions> : IMapDefinition
        where TMapOptions : MapOptions, new()
    {        
        public Type SourceType { get { return typeof(TSource); } }
        public Type DestinationType { get { return typeof(TDest); } }

        public void AddToStack(List<string> stack)
        {
            stack.Add(SourceType.Name);
        }

        public bool Push(MapOptions options)
        {
            if (options.Stack.ToList().Count(_ => _ == SourceType.Name) >= options.MaxRepeatDeepness)
                return false;
            //if (options.Stack.Contains(SourceType.Name))
            //    return false;
            AddToStack(options.Stack);
            options.Deepness++;

            if (options.IsCycleDetected)
                return false;

            return true;
        }

        public async Task<T> SafeMap<T>(Func<Task<T>> map, MapOptions options)
        {
            options = options ?? new MapOptions();

            if (!Push(options))
                return default(T);

            var result = await map();

            Pop(options);

            return result;
        }

        public async Task SafeMap(Func<Task> map, MapOptions options)
        {
            options = options ?? new MapOptions();

            if (!Push(options))
                return;

            await map();

            Pop(options);
        }

        public void Pop(MapOptions options)
        {
            RemoveFromStack(options.Stack);
            options.Deepness--;
        }

        public void RemoveFromStack(List<string> stack)
        {
            stack.Remove(SourceType.Name);
        }

        public async Task Map(TSource source, TSource destination, TMapOptions options = null)
        {
            await SafeMap(() => MapCore(source, destination, options), options);
        }
        public abstract Task MapCore(TSource source, TSource destination, TMapOptions options = null);

        public async Task<TDest> Map(TSource source, TMapOptions options = default)
        {
            return await SafeMap(() => MapCore(source, options), options);
        }
        public abstract Task<TDest> MapCore(TSource source, TMapOptions options = default);
        public virtual async Task<List<TDest>> Map(List<TSource> source, TMapOptions options = default)
        {
            if (source == null)
                return null;

            options = options ?? new TMapOptions();

            var result = new List<TDest>();

            foreach (var sourceItem in source)
                result.Add(await Map(sourceItem, options));

            return result;
        }

        public async Task<TSource> ReverseMap(TDest source, TMapOptions options = default)
        {
            return await SafeMap(() => ReverseMapCore(source, options), options);
        }
        public abstract Task<TSource> ReverseMapCore(TDest source, TMapOptions options = default);
        public virtual async Task<List<TSource>> ReverseMap(List<TDest> source, TMapOptions options = default)
        {
            if (source == null)
                return null;

            options = options ?? new TMapOptions();

            var result = new List<TSource>();
            foreach (var sourceItem in source)
                result.Add(await ReverseMap(sourceItem, options));

            return result;
        }

        public virtual async Task Map(List<TSource> source, List<TSource> destination, TMapOptions options = default)
        {
            if (source == null || destination == null)
                return;

            options = options ?? new TMapOptions();

            var collectionStrategy = options != null ? options.MapCollection : MapOptions.MapCollectionStrategy.ClearAndReplaceItems;

            switch (collectionStrategy)
            {
                case MapOptions.MapCollectionStrategy.ClearAndReplaceItems:
                    destination.Clear();
                    destination.AddRange(source);
                    break;
            }
        }
    }
}
