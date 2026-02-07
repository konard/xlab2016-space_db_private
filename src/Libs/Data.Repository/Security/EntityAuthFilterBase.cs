using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Data.Repository.Security
{
    public abstract class EntityAuthFilterBase<T, TAuthInfo>
        where T : class
        where TAuthInfo : AuthInfoBase
    {
        public bool IsDisabled { get; set; }

        public abstract IQueryable<T> Filter(IQueryable<T> source, TAuthInfo authInfo);
    }
}
