using System;
using System.Collections.Generic;

namespace Data.Repository
{
    public class QueryBase<T, TFilter, TSort> : IPaginable, IQueryBase<T>
        where TFilter : FilterBase<T>
        where TSort : SortBase<T>
    {
        public Paging Paging { get; set; }
        public TFilter Filter { get; set; }
        public FilterOperator? FilterOperator { get; set; }
        public TSort Sort { get; set; }
        public QueryOptions Options { get; set; }
        public List<string> Includes { get; set; }        
        IFilter IQueryBase<T>.Filter { get => Filter; }
        SortBase<T> IQueryBase<T>.Sort { get => Sort; }
    }
}
