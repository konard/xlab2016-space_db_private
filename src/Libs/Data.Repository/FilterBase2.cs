using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repository
{
    public class FilterBase2<T, TAdvancedFilter> : FilterBase<T>
        where TAdvancedFilter : AdvancedFilterBase<T>
    {
        public TAdvancedFilter AdvancedFilter { get; set; }
    }
}
