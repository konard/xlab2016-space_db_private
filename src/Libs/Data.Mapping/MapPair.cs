using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Mapping
{
    public class MapPair<T>
    {
        public T Source { get; set; }
        public T Target { get; set; }
    }
}
