using Data.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository.Security
{
    public abstract class AuthenticatedDataRepositoryBase<T, TQuery, TFilter, TSort, TKey, TAuthInfo, TAuthFilter> : 
        DataRepositoryBase<T, TQuery, TFilter, TSort, TKey>,
        IAuthenticatedRepository<TAuthInfo>
        where T : class, IEntityKey<TKey>
        where TFilter : FilterBase<T>
        where TSort : SortBase<T>
        where TQuery : QueryBase<T, TFilter, TSort>
        where TAuthInfo : AuthInfoBase
        where TAuthFilter : EntityAuthFilterBase<T, TAuthInfo>, new()
    {
        protected TAuthFilter authFilter;
        private readonly IAuthDataService<TAuthInfo> authDataService;

        public TAuthInfo AuthInfo { get; set; }
        
        public bool IsAuthFilterDisabled { get { return authFilter.IsDisabled; } set { authFilter.IsDisabled = value; } }

        public AuthenticatedDataRepositoryBase(IConfiguration configuration,            
            ILogger logger,
            IAuthDataService<TAuthInfo> authDataService) : 
            base(configuration, logger)
        {
            this.authDataService = authDataService;
            authFilter = new TAuthFilter();
        }

        public async Task Authorize()
        {
            AuthInfo = await authDataService.AuthorizeAsync();
        }
    }
}
