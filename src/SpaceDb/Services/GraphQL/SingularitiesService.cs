using HotChocolate;
using HotChocolate.Authorization;
using SpaceDb.Models.Dtos;

namespace SpaceDb.Services.GraphQL
{
    public class SingularitiesService : RestService2<Singularity, long, SingularityDto, SingularityQuery, SingularityMap>
    {
        private readonly SpaceDbContext db;

        public SingularitiesService(ILogger<RestServiceBase<Singularity, long>> logger,
            IDapperDbContext restDapperDb,
            SpaceDbContext restDb,
            SingularityMap map)
            : base(logger,
                restDapperDb,
                restDb,
                "Singularities",
                map)
        {
            this.db = restDb;
        }

        public override async Task<PagedList<SingularityDto>> SearchAsync(SingularityQuery query)
        {
            return await base.SearchAsync(query);
        }
    }
}
