using Api.AspNetCore.Models.Scope;
using Api.AspNetCore.Models.Secure;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Api.AspNetCore.Services
{
    public class AuthorizeService : IAuthorizeService
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public AuthorizeService(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<IAuthorizationData> IsAuthorized()
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            var result = (IAuthorizationData)httpContext.Items[nameof(IAuthorizationData)];
            if (result != null)
                return result;

            result = await ReadFromHttpContext(httpContext);
            httpContext.Items[nameof(IAuthorizationData)] = result;
            return result;
        }

        protected virtual async Task<IAuthorizationData> ReadFromHttpContext(HttpContext httpCtx)
        {
            var userIdClaim = httpCtx.User?.Claims.
                FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var userName = httpCtx.User?.Identity.Name;

            var roles = httpCtx.User?.Claims.
                Where(c => c.Type == ClaimTypes.Role).
                Select(x => x.Value).ToList();
            return new AuthorizationData
            {
                UserId = userIdClaim?.Value,
                UserName = userName,
                Roles = roles,
                Claims = httpCtx.User?.Claims.Select(_ => new AuthorizationData.ClaimSnapshot
                {
                    Type = _.Type,
                    Value = _.Value,
                }).ToList()
            };
        }

        public static (string UserName, string UserId, string Roles) ParseClaims(ClaimsPrincipal principal)
        {
            var name = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var nameIdentifier = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            var roles = principal.Claims.Where(c => c.Type == ClaimTypes.Role).
                Select(x => x.Value).ToList();

            return (name, nameIdentifier, string.Join(',', roles));
        }
    }
}
