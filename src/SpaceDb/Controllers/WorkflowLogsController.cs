using Data.Repository;
using Data.Repository.Dapper;
using SpaceDb.Data.SpaceDb.DatabaseContext;
using SpaceDb.Data.SpaceDb.Entities;
using SpaceDb.Mappings;
using SpaceDb.Models.Dtos;
using SpaceDb.Models.Queries.WorkflowLogs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using System.Net.Mime;

namespace SpaceDb.Controllers
{
    /// <summary>
    /// Лог workflow
    /// </summary>
    [Route("/api/v1/workflowLogs")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdministrator,Administrator")]
    public partial class WorkflowLogsController : RestControllerBase2<WorkflowLog, long, WorkflowLogDto, WorkflowLogQuery, WorkflowLogMap>
    {
        public WorkflowLogsController(ILogger<RestServiceBase<WorkflowLog, long>> logger,
            IDapperDbContext restDapperDb,
            SpaceDbContext restDb,
            WorkflowLogMap workflowLogMap)
            : base(logger,
                restDapperDb,
                restDb,
                "WorkflowLogs",
                workflowLogMap)
        {
        }

        /// <summary>
        /// Search of WorkflowLog using given query
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <response code="200">List of workflowLogs</response>
        /// <response code="400">Validation errors detected, operation denied</response>
        /// <response code="401">Unauthorized request</response>
        [Route("/api/v1/workflowLogs/search")]
        [HttpPost]
        [ProducesResponseType(typeof(PagedList<WorkflowLogDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        public override async Task<PagedList<WorkflowLogDto>> SearchAsync([FromBody] WorkflowLogQuery query)
        {
            return await base.SearchAsync(query);
        }

        /// <summary>
        /// Get the workflowLog by id
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <response code="200">WorkflowLog data</response>
        /// <response code="401">Unauthorized request</response>
        [Route("/api/v1/workflowLogs/{key}")]
        [HttpGet]
        [ProducesResponseType(typeof(WorkflowLogDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        public override async Task<WorkflowLogDto> FindAsync([FromRoute] long key)
        {
            return await base.FindAsync(key);
        }

    }
}
