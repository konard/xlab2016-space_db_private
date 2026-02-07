using Data.Repository;
using Data.Repository.Dapper;
using SpaceDb.Data.SpaceDb.DatabaseContext;
using SpaceDb.Data.SpaceDb.Entities;
using SpaceDb.Mappings;
using SpaceDb.Models.Dtos;
using SpaceDb.Models.Queries.Roles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using System.Net.Mime;

namespace SpaceDb.Controllers
{
    /// <summary>
    /// Роль
    /// </summary>
    [Route("/api/v1/roles")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdministrator,Administrator")]
    public partial class RolesController : RestControllerBase2<Role, int, RoleDto, RoleQuery, RoleMap>
    {
        public RolesController(ILogger<RestServiceBase<Role, int>> logger,
            IDapperDbContext restDapperDb,
            SpaceDbContext restDb,
            RoleMap roleMap)
            : base(logger,
                restDapperDb,
                restDb,
                "Roles",
                roleMap)
        {
        }

        /// <summary>
        /// Search of Role using given query
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <response code="200">List of roles</response>
        /// <response code="400">Validation errors detected, operation denied</response>
        /// <response code="401">Unauthorized request</response>
        [Route("/api/v1/roles/search")]
        [HttpPost]
        [ProducesResponseType(typeof(PagedList<RoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        public override async Task<PagedList<RoleDto>> SearchAsync([FromBody] RoleQuery query)
        {
            return await base.SearchAsync(query);
        }

        /// <summary>
        /// Get the role by id
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <response code="200">Role data</response>
        /// <response code="401">Unauthorized request</response>
        [Route("/api/v1/roles/{key}")]
        [HttpGet]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        public override async Task<RoleDto> FindAsync([FromRoute] int key)
        {
            return await base.FindAsync(key);
        }

    }
}
