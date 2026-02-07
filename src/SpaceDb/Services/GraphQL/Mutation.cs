using HotChocolate;
using HotChocolate.Authorization;
using SpaceDb.Models.Dtos;
using Data.Repository;
using Data.Repository.Dapper;

namespace SpaceDb.Services.GraphQL
{
    public class Mutation
    {
        [Authorize(Roles=["SuperAdministrator", "Administrator"])]
        public async Task<int> CreateUser(UserDto userDto, [Service] UsersService service)
        {
            var result = await service.AddAsync(userDto);
            return (int)result;
        }

        public async Task<bool> UpdateUser(UserDto userDto, [Service] UsersService service)
        {
            var result = await service.UpdateAsync(userDto);
            return (bool)result;
        }

        public async Task<RemoveOperationResult> DeleteUser(int id, [Service] UsersService service)
        {
            var result = await service.RemoveAsync(id);
            return (RemoveOperationResult)result;
        }

        [Authorize(Roles=["SuperAdministrator", "Administrator"])]
        public async Task<int> CreateRole(RoleDto roleDto, [Service] RolesService service)
        {
            var result = await service.AddAsync(roleDto);
            return (int)result;
        }

        public async Task<bool> UpdateRole(RoleDto roleDto, [Service] RolesService service)
        {
            var result = await service.UpdateAsync(roleDto);
            return (bool)result;
        }

        public async Task<RemoveOperationResult> DeleteRole(int id, [Service] RolesService service)
        {
            var result = await service.RemoveAsync(id);
            return (RemoveOperationResult)result;
        }

        [Authorize(Roles=["SuperAdministrator", "Administrator"])]
        public async Task<int> CreateUserRole(UserRoleDto userRoleDto, [Service] UserRolesService service)
        {
            var result = await service.AddAsync(userRoleDto);
            return (int)result;
        }

        public async Task<bool> UpdateUserRole(UserRoleDto userRoleDto, [Service] UserRolesService service)
        {
            var result = await service.UpdateAsync(userRoleDto);
            return (bool)result;
        }

        public async Task<RemoveOperationResult> DeleteUserRole(int id, [Service] UserRolesService service)
        {
            var result = await service.RemoveAsync(id);
            return (RemoveOperationResult)result;
        }

        [Authorize(Roles=["SuperAdministrator", "Administrator"])]
        public async Task<int> CreateTenant(TenantDto tenantDto, [Service] TenantsService service)
        {
            var result = await service.AddAsync(tenantDto);
            return (int)result;
        }

        public async Task<bool> UpdateTenant(TenantDto tenantDto, [Service] TenantsService service)
        {
            var result = await service.UpdateAsync(tenantDto);
            return (bool)result;
        }

        public async Task<RemoveOperationResult> DeleteTenant(int id, [Service] TenantsService service)
        {
            var result = await service.RemoveAsync(id);
            return (RemoveOperationResult)result;
        }

        [Authorize(Roles=["SuperAdministrator", "Administrator"])]
        public async Task<int> CreateSingularity(SingularityDto singularityDto, [Service] SingularitiesService service)
        {
            var result = await service.AddAsync(singularityDto);
            return (int)result;
        }

        public async Task<bool> UpdateSingularity(SingularityDto singularityDto, [Service] SingularitiesService service)
        {
            var result = await service.UpdateAsync(singularityDto);
            return (bool)result;
        }

        public async Task<RemoveOperationResult> DeleteSingularity(int id, [Service] SingularitiesService service)
        {
            var result = await service.RemoveAsync(id);
            return (RemoveOperationResult)result;
        }
    }
}
