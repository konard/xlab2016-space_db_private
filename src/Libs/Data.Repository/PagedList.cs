using System;
using System.Collections.Generic;
using System.Linq;

namespace Data.Repository
{
    public class PagedList<T>
    {
        public List<T> Result { get; set; }
        public int? Total { get; set; }
        public int? PageCount { get; set; }

        public PagedList()
        {
        }

        public PagedList(IEnumerable<T> result, int? count, Paging paging)
        {
            Result = result?.ToList();
            Total = count;

            if (paging != null)
                Calculate(paging);
        }

        public PagedList(PagedList<T> original)
        {
            Result = original.Result;
            Total = original.Total;
            PageCount = original.PageCount;
        }

        public void Calculate(Paging paging)
        {
            if (!Total.HasValue || !(paging?.PageSize).HasValue)
                return;

            PageCount = Total / paging.PageSize;

            if (Total % paging.PageSize != 0)
                PageCount++;
        }

        public PagedList<U> Map<U>(Func<T, U> mapping)
        {
            return new PagedList<U>
            {
                Result = Result.Select(_ => mapping(_)).ToList(),
                Total = Total,
                PageCount = PageCount
            };
        }
    }
}
