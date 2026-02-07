using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Mapping
{
    public class MapOptions
    {
        public const int MaxDeepness = 10;
        public int MaxRepeatDeepness = 1;

        public bool MapProperties { get; set; }
        public bool MapObjects { get; set; }
        public bool MapCollections { get; set; }
        public MapCollectionStrategy MapCollection { get; set; }
        public List<string> Stack { get; set; }
        public int Deepness { get; set; }

        public bool IsCycleDetected => Deepness >= MaxDeepness;

        public MapOptions()
        {
            MapProperties = true;
            MapObjects = true;
            MapCollections = true;
            MapCollection = MapCollectionStrategy.ClearAndReplaceItems;
            Stack = new List<string>();
        }

        public enum MapCollectionStrategy
        {
            None,
            ClearAndReplaceItems
        }
    }
}
