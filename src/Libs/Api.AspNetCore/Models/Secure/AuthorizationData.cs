using Api.AspNetCore.Models.Scope;
using Data.Repository.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Api.AspNetCore.Models.Secure
{
    public class AuthorizationData : IAuthorizationData
    {
        public string UserId { get; set; }
        public string UserName { get; set; }

        public List<string> Roles { get; set; }

        public List<ClaimSnapshot> Claims { get; set; }

        public AuthorizationData()
        {
        }

        public AuthorizationData(string userId, string userName, List<string> roles)
        {
            UserId = userId;
            UserName = userName;
            Roles = roles;
        }

        public string FindClaimValue(string type)
        {
            return Claims.
                FirstOrDefault(_ => _.Type == type)?.Value;
        }

        public long? FindClaimValueLong(string type)
        {
            var value = Claims.
                FirstOrDefault(_ => _.Type == type)?.Value;
            long result;
            if (long.TryParse(value, out result))
                return result;
            return null;
        }

        public int? FindClaimValueInt(string type)
        {
            var value = Claims.
                FirstOrDefault(_ => _.Type == type)?.Value;
            int result;
            if (int.TryParse(value, out result))
                return result;
            return null;
        }


        public T Extend<T>()
            where T : IAuthorizationData, new()
        {
            return JsonHelper.Extend<IAuthorizationData, T>(this, (source, target) =>
            {
                target.UserId = source.UserId;
                target.UserName = source.UserName;
                target.Roles = source.Roles;
                target.Claims = source.Claims;
            });
        }

        public class ClaimSnapshot
        {
            public string Type { get; set; }
            public string Value { get; set; }
        }
    }
}
