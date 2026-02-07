using Data.Repository;
using Data.Repository.Dapper;
using SpaceDb.Data.SpaceDb.DatabaseContext;
using SpaceDb.Data.SpaceDb.Entities;
using SpaceDb.Mappings;
using SpaceDb.Models.Dtos;
using SpaceDb.Models.Queries.UserRoles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using System.Net.Mime;
using Microsoft.EntityFrameworkCore;

namespace SpaceDb.Controllers
{
    /// <summary>
    /// Присвоенная роль
    /// </summary>
    [Route("/api/v1/userRoles")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdministrator,Administrator")]
    public partial class UserRolesController : RestControllerBase2<UserRole, int, UserRoleDto, UserRoleQuery, UserRoleMap>
    {
        public UserRolesController(ILogger<RestServiceBase<UserRole, int>> logger,
            IDapperDbContext restDapperDb,
            SpaceDbContext restDb,
            UserRoleMap userRoleMap)
            : base(logger,
                restDapperDb,
                restDb,
                "UserRoles",
                userRoleMap)
        {
        }

        /// <summary>
        /// Search of UserRole using given query
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <response code="200">List of userRoles</response>
        /// <response code="400">Validation errors detected, operation denied</response>
        /// <response code="401">Unauthorized request</response>
        [Route("/api/v1/userRoles/search")]
        [HttpPost]
        [ProducesResponseType(typeof(PagedList<UserRoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        public override async Task<PagedList<UserRoleDto>> SearchAsync([FromBody] UserRoleQuery query)
        {
            return await SearchUsingEfAsync(query, _ => _.
                Include(_ => _.User).
                Include(_ => _.Role));
        }

        /// <summary>
        /// Get the userRole by id
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <response code="200">UserRole data</response>
        /// <response code="401">Unauthorized request</response>
        [Route("/api/v1/userRoles/{key}")]
        [HttpGet]
        [ProducesResponseType(typeof(UserRoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        public override async Task<UserRoleDto> FindAsync([FromRoute] int key)
        {
            return await FindUsingEfAsync(key, _ => _.
                Include(_ => _.User).
                Include(_ => _.Role));
        }

    }
}
