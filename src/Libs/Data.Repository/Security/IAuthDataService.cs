using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository.Security
{
    public interface IAuthDataService<TAuthInfo>
        where TAuthInfo : AuthInfoBase
    {
        Task<TAuthInfo> AuthorizeAsync();
    }
}
