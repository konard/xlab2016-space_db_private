using Data.Repository;
using Data.Repository.Dapper;
using SpaceDb.Data.SpaceDb.DatabaseContext;
using SpaceDb.Data.SpaceDb.Entities;
using SpaceDb.Mappings;
using SpaceDb.Models.Dtos;
using SpaceDb.Models.Queries.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using System.Net.Mime;
using Microsoft.EntityFrameworkCore;

namespace SpaceDb.Controllers
{
    /// <summary>
    /// Пользователь
    /// </summary>
    [Route("/api/v1/users")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdministrator,Administrator")]
    public partial class UsersController : RestControllerBase2<User, int, UserDto, UserQuery, UserMap>
    {
        public UsersController(ILogger<RestServiceBase<User, int>> logger,
            IDapperDbContext restDapperDb,
            SpaceDbContext restDb,
            UserMap userMap)
            : base(logger,
                restDapperDb,
                restDb,
                "Users",
                userMap)
        {
        }

        /// <summary>
        /// Search of User using given query
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <response code="200">List of users</response>
        /// <response code="400">Validation errors detected, operation denied</response>
        /// <response code="401">Unauthorized request</response>
        [Route("/api/v1/users/search")]
        [HttpPost]
        [ProducesResponseType(typeof(PagedList<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        public override async Task<PagedList<UserDto>> SearchAsync([FromBody] UserQuery query)
        {
            return await SearchUsingEfAsync(query, _ => _.
                Include(_ => _.Roles));
        }

        /// <summary>
        /// Get the user by id
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <response code="200">User data</response>
        /// <response code="401">Unauthorized request</response>
        [Route("/api/v1/users/{key}")]
        [HttpGet]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        public override async Task<UserDto> FindAsync([FromRoute] int key)
        {
            return await FindUsingEfAsync(key, _ => _.
                Include(_ => _.Roles));
        }

    }
}
