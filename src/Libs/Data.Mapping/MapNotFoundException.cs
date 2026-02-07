using System;

namespace Data.Mapping
{
    public class MapNotFoundException : Exception
    {
        public MapNotFoundException(Type sourceType, Type destinationType)
            : base(sourceType.Name + "=>" + destinationType.Name)
        {
        }
    }
}
