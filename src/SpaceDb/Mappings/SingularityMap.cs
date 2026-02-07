using Data.Mapping;
using SpaceDb.Data.SpaceDb.Entities;
using SpaceDb.Models.Dtos;

namespace SpaceDb.Mappings
{
    /// <summary>
    /// Сингулярность
    /// </summary>
    public partial class SingularityMap : MapBase2<Singularity, SingularityDto, MapOptions>
    {
        private readonly DbMapContext mapContext;

        public SingularityMap(DbMapContext mapContext)
        {
            this.mapContext = mapContext;
        }

        public override SingularityDto MapCore(Singularity source, MapOptions? options = null)
        {
            if (source == null)
                return null;

            options = options ?? new MapOptions();

            var result = new SingularityDto();
            result.Id = source.Id;
            if (options.MapProperties)
            {
                result.Name = source.Name;
                result.Version = source.Version;
                result.Private = source.Private;
            }
            if (options.MapObjects)
            {
            }
            if (options.MapCollections)
            {
            }

            return result;
        }

        public override Singularity ReverseMapCore(SingularityDto source, MapOptions options = null)
        {
            if (source == null)
                return null;

            options = options ?? new MapOptions();

            var result = new Singularity();
            result.Id = source.Id;
            if (options.MapProperties)
            {
                result.Name = source.Name;
                result.Version = source.Version;
                result.Private = source.Private;
            }
            if (options.MapObjects)
            {
            }
            if (options.MapCollections)
            {
            }

            return result;
        }

        public override void MapCore(Singularity source, Singularity destination, MapOptions options = null)
        {
            if (source == null || destination == null)
                return;

            options = options ?? new MapOptions();

            destination.Id = source.Id;
            if (options.MapProperties)
            {
                destination.Name = source.Name;
                destination.Version = source.Version;
                destination.Private = source.Private;
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
