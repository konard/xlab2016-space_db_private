using HotChocolate;
using HotChocolate.Authorization;
using SpaceDb.Models.Dtos;

namespace SpaceDb.Services.GraphQL
{
    public class UserRolesService : RestService2<UserRole, int, UserRoleDto, UserRoleQuery, UserRoleMap>
    {
        private readonly SpaceDbContext db;

        public UserRolesService(ILogger<RestServiceBase<UserRole, int>> logger,
            IDapperDbContext restDapperDb,
            SpaceDbContext restDb,
            UserRoleMap map)
            : base(logger,
                restDapperDb,
                restDb,
                "UserRoles",
                map)
        {
            this.db = restDb;
        }

        public override async Task<PagedList<UserRoleDto>> SearchAsync(UserRoleQuery query)
        {
            return await SearchUsingEfAsync(query, _ => _.
                Include(_ => _.User).
                Include(_ => _.Role));
        }
    }
}
