using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Mapping
{
    public abstract class MapContextBase
    {
        public IMapper Mapper { get; set; }
    }
}
