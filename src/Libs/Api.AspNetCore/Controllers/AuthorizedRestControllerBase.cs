using Api.AspNetCore.Services;
using Data.Repository.Security;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Api.AspNetCore.Controllers
{
    public class AuthorizedRestControllerBase<TAuthInfo> : RestControllerBase
        where TAuthInfo : AuthInfoBase
    {
        protected readonly IAuthenticatedRepository<TAuthInfo> authenticatedRepository;

        public AuthorizedRestControllerBase()
        {
        }

        public AuthorizedRestControllerBase(IAuthorizeService authorizeService)
            : base(authorizeService)
        {
        }

        public AuthorizedRestControllerBase(IAuthorizeService authorizeService, 
            IAuthenticatedRepository<TAuthInfo> authenticatedRepository)
            : base(authorizeService)
        {
            this.authenticatedRepository = authenticatedRepository;
        }

        protected override async Task<bool> AuthorizeCore()
        {
            if (authenticatedRepository == null)
                return await base.AuthorizeCore();
            await authenticatedRepository.Authorize();

            return !string.IsNullOrEmpty(authenticatedRepository.AuthInfo?.UserName);
        }
    }
}
