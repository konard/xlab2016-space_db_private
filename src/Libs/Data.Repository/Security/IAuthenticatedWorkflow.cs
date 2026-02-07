using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository.Security
{
    public interface IAuthenticatedWorkflow<TAuthInfo>
        where TAuthInfo : AuthInfoBase
    {
        public TAuthInfo AuthInfo { get; set; }

        Task Authorize();
    }
}
