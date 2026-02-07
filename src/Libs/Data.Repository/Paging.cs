using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repository
{
    public class Paging
    {
        public int? Skip { get; set; }
        public int? Take { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool ReturnCount { get; set; }

        public void Calculate()
        {
            if (PageSize.HasValue && PageNumber.HasValue)
            {
                Take = PageSize;
                Skip = PageSize * (PageNumber - 1);
            }
        }
    }
}
