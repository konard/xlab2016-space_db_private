using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository.Security
{
    public interface IAuthenticatedRepository<TAuthInfo>
        where TAuthInfo : AuthInfoBase
    {
        public TAuthInfo AuthInfo { get; set; }
        public bool IsAuthFilterDisabled { get; set; }

        Task Authorize();
    }
}
