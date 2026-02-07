using Data.Mapping;
using SpaceDb.Data.SpaceDb.Entities;
using SpaceDb.Models.Dtos;

namespace SpaceDb.Mappings
{
    /// <summary>
    /// Присвоенная роль
    /// </summary>
    public partial class UserRoleMap : MapBase2<UserRole, UserRoleDto, MapOptions>
    {
        private readonly DbMapContext mapContext;

        public UserRoleMap(DbMapContext mapContext)
        {
            this.mapContext = mapContext;
        }

        public override UserRoleDto MapCore(UserRole source, MapOptions? options = null)
        {
            if (source == null)
                return null;

            options = options ?? new MapOptions();

            var result = new UserRoleDto();
            result.Id = source.Id;
            if (options.MapProperties)
            {
                result.UserId = source.UserId;
                result.RoleId = source.RoleId;
            }
            if (options.MapObjects)
            {
                result.User = mapContext.UserMap.Map(source.User, options);
                result.Role = mapContext.RoleMap.Map(source.Role, options);
            }
            if (options.MapCollections)
            {
            }

            return result;
        }

        public override UserRole ReverseMapCore(UserRoleDto source, MapOptions options = null)
        {
            if (source == null)
                return null;

            options = options ?? new MapOptions();

            var result = new UserRole();
            result.Id = source.Id;
            if (options.MapProperties)
            {
                result.UserId = source.UserId;
                result.RoleId = source.RoleId;
            }
            if (options.MapObjects)
            {
                if (source.UserId == null)
                    result.User = mapContext.UserMap.ReverseMap(source.User, options);
                if (source.RoleId == null)
                    result.Role = mapContext.RoleMap.ReverseMap(source.Role, options);
            }
            if (options.MapCollections)
            {
            }

            return result;
        }

        public override void MapCore(UserRole source, UserRole destination, MapOptions options = null)
        {
            if (source == null || destination == null)
                return;

            options = options ?? new MapOptions();

            destination.Id = source.Id;
            if (options.MapProperties)
            {
                destination.UserId = source.UserId;
                destination.RoleId = source.RoleId;
            }
            if (options.MapObjects)
            {
            }
            if (options.MapCollections)
            {
            }

        }
    }
}
