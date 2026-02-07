using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Mapping
{
    public abstract class MapBase<TSource, TDest, TMapOptions> : IMapDefinition
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
            if (options.Stack.Contains(SourceType.Name))
                return false;
            AddToStack(options.Stack);
            options.Deepness++;

            if (options.IsCycleDetected)
                return false;

            return true;
        }

        public T SafeMap<T>(Func<T> map, MapOptions options)
        {
            if (!Push(options))
                return default(T);

            var result = map();

            Pop(options);

            return result;
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

        public abstract void Map(TSource source, TSource destination, TMapOptions options = null);

        public abstract TDest Map(TSource source, TMapOptions options = default);
        public virtual List<TDest> Map(List<TSource> source, TMapOptions options = default)
        {
            if (source == null)
                return null;

            options = options ?? new TMapOptions();

            var result = new List<TDest>();

            foreach (var sourceItem in source)
                result.Add(Map(sourceItem, options));

            return result;
        }

        public abstract TSource ReverseMap(TDest source, TMapOptions options = default);
        public virtual List<TSource> ReverseMap(List<TDest> source, TMapOptions options = default)
        {
            if (source == null)
                return null;

            options = options ?? new TMapOptions();

            var result = new List<TSource>();
            foreach (var sourceItem in source)
                result.Add(ReverseMap(sourceItem, options));

            return result;
        }

        public virtual void Map(List<TSource> source, List<TSource> destination, TMapOptions options = default)
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
