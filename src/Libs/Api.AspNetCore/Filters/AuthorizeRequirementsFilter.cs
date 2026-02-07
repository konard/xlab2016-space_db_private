using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Security.Claims;

namespace Api.AspNetCore.Filters
{
    public class AuthorizeRequirementsFilter : IAuthorizationFilter
    {
        private readonly AuthorizeRequirementOptions options;

        public AuthorizeRequirementsFilter(AuthorizeRequirementOptions options)
        {
            this.options = options;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (options.IsAuthenticated && !context.HttpContext.User.Identity.IsAuthenticated)
            {
                context.Result = new ForbidResult();
                return;
            }

            if (options.Roles != null && !context.HttpContext.User.Claims.Any(c => c.Type == ClaimTypes.Role && options.Roles.Contains(c.Value)))
            {
                context.Result = new ForbidResult();
                return;
            }

            if (options.DenyRoles != null && context.HttpContext.User.Claims.Any(c => c.Type == ClaimTypes.Role && options.DenyRoles.Contains(c.Value)))
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}
