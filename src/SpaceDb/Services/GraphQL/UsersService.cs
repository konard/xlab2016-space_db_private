using HotChocolate;
using HotChocolate.Authorization;
using SpaceDb.Models.Dtos;

namespace SpaceDb.Services.GraphQL
{
    public class UsersService : RestService2<User, int, UserDto, UserQuery, UserMap>
    {
        private readonly SpaceDbContext db;

        public UsersService(ILogger<RestServiceBase<User, int>> logger,
            IDapperDbContext restDapperDb,
            SpaceDbContext restDb,
            UserMap map)
            : base(logger,
                restDapperDb,
                restDb,
                "Users",
                map)
        {
            this.db = restDb;
        }

        public override async Task<PagedList<UserDto>> SearchAsync(UserQuery query)
        {
            return await SearchUsingEfAsync(query, _ => _.
                Include(_ => _.Roles));
        }
    }
}
