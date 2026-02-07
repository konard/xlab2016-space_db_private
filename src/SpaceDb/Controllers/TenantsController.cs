using Data.Repository;
using Data.Repository.Dapper;
using SpaceDb.Data.SpaceDb.DatabaseContext;
using SpaceDb.Data.SpaceDb.Entities;
using SpaceDb.Mappings;
using SpaceDb.Models.Dtos;
using SpaceDb.Models.Queries.Tenants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using System.Net.Mime;

namespace SpaceDb.Controllers
{
    /// <summary>
    /// Организация
    /// </summary>
    [Route("/api/v1/tenants")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdministrator,Administrator")]
    public partial class TenantsController : RestControllerBase2<Tenant, int, TenantDto, TenantQuery, TenantMap>
    {
        public TenantsController(ILogger<RestServiceBase<Tenant, int>> logger,
            IDapperDbContext restDapperDb,
            SpaceDbContext restDb,
            TenantMap tenantMap)
            : base(logger,
                restDapperDb,
                restDb,
                "Tenants",
                tenantMap)
        {
        }

        /// <summary>
        /// Search of Tenant using given query
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <response code="200">List of tenants</response>
        /// <response code="400">Validation errors detected, operation denied</response>
        /// <response code="401">Unauthorized request</response>
        [Route("/api/v1/tenants/search")]
        [HttpPost]
        [ProducesResponseType(typeof(PagedList<TenantDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        public override async Task<PagedList<TenantDto>> SearchAsync([FromBody] TenantQuery query)
        {
            return await base.SearchAsync(query);
        }

        /// <summary>
        /// Get the tenant by id
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <response code="200">Tenant data</response>
        /// <response code="401">Unauthorized request</response>
        [Route("/api/v1/tenants/{key}")]
        [HttpGet]
        [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        public override async Task<TenantDto> FindAsync([FromRoute] int key)
        {
            return await base.FindAsync(key);
        }

    }
}
