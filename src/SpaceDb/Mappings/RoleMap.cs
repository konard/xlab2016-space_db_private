using Data.Mapping;
using SpaceDb.Data.SpaceDb.Entities;
using SpaceDb.Models.Dtos;

namespace SpaceDb.Mappings
{
    /// <summary>
    /// Роль
    /// </summary>
    public partial class RoleMap : MapBase2<Role, RoleDto, MapOptions>
    {
        private readonly DbMapContext mapContext;

        public RoleMap(DbMapContext mapContext)
        {
            this.mapContext = mapContext;
        }

        public override RoleDto MapCore(Role source, MapOptions? options = null)
        {
            if (source == null)
                return null;

            options = options ?? new MapOptions();

            var result = new RoleDto();
            result.Id = source.Id;
            if (options.MapProperties)
            {
                result.Name = source.Name;
                result.Code = source.Code;
            }
            if (options.MapObjects)
            {
            }
            if (options.MapCollections)
            {
            }

            return result;
        }

        public override Role ReverseMapCore(RoleDto source, MapOptions options = null)
        {
            if (source == null)
                return null;

            options = options ?? new MapOptions();

            var result = new Role();
            result.Id = source.Id;
            if (options.MapProperties)
            {
                result.Name = source.Name;
                result.Code = source.Code;
            }
            if (options.MapObjects)
            {
            }
            if (options.MapCollections)
            {
            }

            return result;
        }

        public override void MapCore(Role source, Role destination, MapOptions options = null)
        {
            if (source == null || destination == null)
                return;

            options = options ?? new MapOptions();

            destination.Id = source.Id;
            if (options.MapProperties)
            {
                destination.Name = source.Name;
                destination.Code = source.Code;
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
