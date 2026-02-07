using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Mapping
{
    public interface IMapDefinition
    {
        public Type SourceType { get; }
        public Type DestinationType { get; }
    }
}
