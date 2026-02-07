using HotChocolate;
using HotChocolate.Authorization;
using SpaceDb.Models.Dtos;

namespace SpaceDb.Services.GraphQL
{
    public class TenantsService : RestService2<Tenant, int, TenantDto, TenantQuery, TenantMap>
    {
        private readonly SpaceDbContext db;

        public TenantsService(ILogger<RestServiceBase<Tenant, int>> logger,
            IDapperDbContext restDapperDb,
            SpaceDbContext restDb,
            TenantMap map)
            : base(logger,
                restDapperDb,
                restDb,
                "Tenants",
                map)
        {
            this.db = restDb;
        }

        public override async Task<PagedList<TenantDto>> SearchAsync(TenantQuery query)
        {
            return await base.SearchAsync(query);
        }
    }
}
