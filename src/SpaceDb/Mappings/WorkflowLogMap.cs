using Data.Mapping;
using Data.Repository.Helpers;
using SpaceDb.Data.SpaceDb.Entities;
using SpaceDb.Models.Dtos;

namespace SpaceDb.Mappings
{
    /// <summary>
    /// Лог workflow
    /// </summary>
    public partial class WorkflowLogMap : MapBase2<WorkflowLog, WorkflowLogDto, MapOptions>
    {
        private readonly DbMapContext mapContext;

        public WorkflowLogMap(DbMapContext mapContext)
        {
            this.mapContext = mapContext;
        }

        public override WorkflowLogDto MapCore(WorkflowLog source, MapOptions? options = null)
        {
            if (source == null)
                return null;

            options = options ?? new MapOptions();

            var result = new WorkflowLogDto();
            result.Id = source.Id;
            if (options.MapProperties)
            {
                result.WorkflowId = source.WorkflowId;
                result.Message = source.Message;
                result.Time = source.Time;
                result.Severity = source.Severity;
            }
            if (options.MapObjects)
            {
            }
            if (options.MapCollections)
            {
            }

            return result;
        }

        public override WorkflowLog ReverseMapCore(WorkflowLogDto source, MapOptions options = null)
        {
            if (source == null)
                return null;

            options = options ?? new MapOptions();

            var result = new WorkflowLog();
            result.Id = source.Id;
            if (options.MapProperties)
            {
                result.WorkflowId = source.WorkflowId;
                result.Message = source.Message;
                result.Time = source.Time.ToUtc();
                result.Severity = source.Severity;
            }
            if (options.MapObjects)
            {
            }
            if (options.MapCollections)
            {
            }

            return result;
        }

        public override void MapCore(WorkflowLog source, WorkflowLog destination, MapOptions options = null)
        {
            if (source == null || destination == null)
                return;

            options = options ?? new MapOptions();

            destination.Id = source.Id;
            if (options.MapProperties)
            {
                destination.WorkflowId = source.WorkflowId;
                destination.Message = source.Message;
                destination.Time = source.Time;
                destination.Severity = source.Severity;
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
