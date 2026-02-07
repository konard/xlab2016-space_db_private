using System.Collections.Generic;

namespace Api.AspNetCore.Filters
{
    public class AuthorizeRequirementOptions
    {
        public bool IsAuthenticated { get; set; }
        public List<string> Roles { get; set; }
        public List<string> DenyRoles { get; set; }
    }
}
