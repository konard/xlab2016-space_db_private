using Api.AspNetCore.Models.Secure;
using Api.AspNetCore.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Api.AspNetCore.Models.Data
{
    public abstract class AuthorizedDbContextBase<TDbContext, TAuthorizationData>
        where TDbContext : DbContext
        where TAuthorizationData : AuthorizationData
    {
        protected TAuthorizationData authorizeData;
        protected readonly IAuthorizeService authorizeService;
        protected readonly TDbContext db;

        public TAuthorizationData AuthorizeData
        {
            get { return authorizeData; }
        }

        public AuthorizedDbContextBase(IAuthorizeService authorizeService, TDbContext db)
        {
            this.authorizeService = authorizeService;
            this.db = db;
        }

        public async Task IsAuthorized()
        {
            authorizeData = (TAuthorizationData)(await authorizeService.IsAuthorized());
        }

        protected virtual IQueryable<T> Authorize<T>(IQueryable<T> source,
            Func<IQueryable<T>, IQueryable<T>> authorizationCore,
            Dictionary<int, Func<IQueryable<T>, IQueryable<T>>> roleAuthorizations, bool allowAnonymous = false)
            where T : class
        {            
            return source;
        }
    }
}
