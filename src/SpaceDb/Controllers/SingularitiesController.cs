using Data.Repository;
using Data.Repository.Dapper;
using SpaceDb.Data.SpaceDb.DatabaseContext;
using SpaceDb.Data.SpaceDb.Entities;
using SpaceDb.Mappings;
using SpaceDb.Models.Dtos;
using SpaceDb.Models.Queries.Singularities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using System.Net.Mime;

namespace SpaceDb.Controllers
{
    /// <summary>
    /// Сингулярность
    /// </summary>
    [Route("/api/v1/singularities")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdministrator,Administrator")]
    public partial class SingularitiesController : RestControllerBase2<Singularity, long, SingularityDto, SingularityQuery, SingularityMap>
    {
        public SingularitiesController(ILogger<RestServiceBase<Singularity, long>> logger,
            IDapperDbContext restDapperDb,
            SpaceDbContext restDb,
            SingularityMap singularityMap)
            : base(logger,
                restDapperDb,
                restDb,
                "Singularities",
                singularityMap)
        {
        }

        /// <summary>
        /// Search of Singularity using given query
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <response code="200">List of singularities</response>
        /// <response code="400">Validation errors detected, operation denied</response>
        /// <response code="401">Unauthorized request</response>
        [Route("/api/v1/singularities/search")]
        [HttpPost]
        [ProducesResponseType(typeof(PagedList<SingularityDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        public override async Task<PagedList<SingularityDto>> SearchAsync([FromBody] SingularityQuery query)
        {
            return await base.SearchAsync(query);
        }

        /// <summary>
        /// Get the singularity by id
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <response code="200">Singularity data</response>
        /// <response code="401">Unauthorized request</response>
        [Route("/api/v1/singularities/{key}")]
        [HttpGet]
        [ProducesResponseType(typeof(SingularityDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        public override async Task<SingularityDto> FindAsync([FromRoute] long key)
        {
            return await base.FindAsync(key);
        }

    }
}
