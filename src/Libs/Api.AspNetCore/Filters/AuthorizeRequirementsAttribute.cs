using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Api.AspNetCore.Filters
{
    public class AuthorizeRequirementsAttribute : TypeFilterAttribute
    {
        public AuthorizeRequirementsAttribute(string roles = null, string denyRoles = null, bool isAuthenticated = true) : 
            base(typeof(AuthorizeRequirementsFilter))
        {
            Roles = roles;
            DenyRoles = denyRoles;
            IsAuthenticated = isAuthenticated;
            Arguments = new object[] { new AuthorizeRequirementOptions
            {
                IsAuthenticated = isAuthenticated,
                Roles = roles?.Split(',').ToList(),
                DenyRoles = denyRoles?.Split(',').ToList()
            } };
        }

        public string Roles { get; }
        public string DenyRoles { get; }
        public bool IsAuthenticated { get; }
    }
}
