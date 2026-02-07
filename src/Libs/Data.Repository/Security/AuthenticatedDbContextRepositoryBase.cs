using Data.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository.Security
{
    public abstract class AuthenticatedDbContextRepositoryBase<T, TQuery, TFilter, TSort, TKey, TDto, TMap, TMapOptions, TAuthInfo, TAuthFilter> : 
        DbContextRepositoryBase<T, TQuery, TFilter, TSort, TKey, TDto, TMap, TMapOptions>,
        IAuthenticatedRepository<TAuthInfo>
        where T : class, IEntityKey<TKey>
        where TFilter : FilterBase<T>
        where TSort : SortBase<T>
        where TQuery : QueryBase<T, TFilter, TSort>
        where TMap : MapBase<T, TDto, TMapOptions>
        where TMapOptions : MapOptions, new()
        where TAuthInfo : AuthInfoBase
        where TAuthFilter : EntityAuthFilterBase<T, TAuthInfo>, new()
    {
        protected TAuthFilter authFilter;
        private readonly IAuthDataService<TAuthInfo> authDataService;

        public TAuthInfo AuthInfo { get; set; }
        
        public bool IsAuthFilterDisabled { get { return authFilter.IsDisabled; } set { authFilter.IsDisabled = value; } }

        public AuthenticatedDbContextRepositoryBase(DbContext db,
            ILogger<AuthenticatedDbContextRepositoryBase<T, TQuery, TFilter, TSort, TKey, TDto, TMap, TMapOptions, TAuthInfo, TAuthFilter>> logger,
            TMap map,
            IAuthDataService<TAuthInfo> authDataService) : 
            base(db, logger, map)
        {
            this.authDataService = authDataService;
            authFilter = new TAuthFilter();
        }

        public override IQueryable<T> All()
        {
            if (AuthInfo == null || authFilter == null)
                return base.All();

            return !authFilter.IsDisabled ? 
                authFilter.Filter(base.All(), AuthInfo) :
                base.All();
        }

        public async Task Authorize()
        {
            AuthInfo = await authDataService.AuthorizeAsync();
        }
    }
}
