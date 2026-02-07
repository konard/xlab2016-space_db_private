using HotChocolate;
using HotChocolate.Authorization;
using SpaceDb.Models.Dtos;

namespace SpaceDb.Services.GraphQL
{
    public class Query
    {
        [Authorize(Roles=["SuperAdministrator", "Administrator"])]
        public async Task<PagedList<UserDto>> Users(UserQuery query, [GlobalState("currentUser")] ClaimsPrincipal user, [Service] UsersService service)
        {
            return await service.SearchAsync(query);
        }

        [Authorize(Roles=["SuperAdministrator", "Administrator"])]
        public async Task<PagedList<RoleDto>> Roles(RoleQuery query, [GlobalState("currentUser")] ClaimsPrincipal user, [Service] RolesService service)
        {
            return await service.SearchAsync(query);
        }

        [Authorize(Roles=["SuperAdministrator", "Administrator"])]
        public async Task<PagedList<UserRoleDto>> UserRoles(UserRoleQuery query, [GlobalState("currentUser")] ClaimsPrincipal user, [Service] UserRolesService service)
        {
            return await service.SearchAsync(query);
        }

        [Authorize(Roles=["SuperAdministrator", "Administrator"])]
        public async Task<PagedList<TenantDto>> Tenants(TenantQuery query, [GlobalState("currentUser")] ClaimsPrincipal user, [Service] TenantsService service)
        {
            return await service.SearchAsync(query);
        }

        [Authorize(Roles=["SuperAdministrator", "Administrator"])]
        public async Task<PagedList<SingularityDto>> Singularities(SingularityQuery query, [GlobalState("currentUser")] ClaimsPrincipal user, [Service] SingularitiesService service)
        {
            return await service.SearchAsync(query);
        }
    }
}
