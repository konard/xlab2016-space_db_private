using Api.AspNetCore.Models.Secure;
using System.Collections.Generic;

namespace Api.AspNetCore.Models.Scope
{
    public interface IAuthorizationData
    {
        string UserId { get; set; }
        string UserName { get; set; }
        List<string> Roles { get; set; }
        List<AuthorizationData.ClaimSnapshot> Claims { get; set; }

        string FindClaimValue(string type);
        long? FindClaimValueLong(string type);
        int? FindClaimValueInt(string type);

        T Extend<T>()
            where T : IAuthorizationData, new();
    }
}
