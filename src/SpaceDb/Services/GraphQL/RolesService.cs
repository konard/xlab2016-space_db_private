using HotChocolate;
using HotChocolate.Authorization;
using SpaceDb.Models.Dtos;

namespace SpaceDb.Services.GraphQL
{
    public class RolesService : RestService2<Role, int, RoleDto, RoleQuery, RoleMap>
    {
        private readonly SpaceDbContext db;

        public RolesService(ILogger<RestServiceBase<Role, int>> logger,
            IDapperDbContext restDapperDb,
            SpaceDbContext restDb,
            RoleMap map)
            : base(logger,
                restDapperDb,
                restDb,
                "Roles",
                map)
        {
            this.db = restDb;
        }

        public override async Task<PagedList<RoleDto>> SearchAsync(RoleQuery query)
        {
            return await base.SearchAsync(query);
        }
    }
}
