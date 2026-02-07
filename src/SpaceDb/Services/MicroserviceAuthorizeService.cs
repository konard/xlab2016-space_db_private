using SpaceDb.Models;
using Api.AspNetCore.Models.Scope;
using Api.AspNetCore.Services;

namespace SpaceDb.Services
{
    public class MicroserviceAuthorizeService : AuthorizeService
    {
        public MicroserviceAuthorizeService(IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
        }
    }
}
