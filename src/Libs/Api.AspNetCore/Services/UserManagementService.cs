using Api.AspNetCore.Models.Secure;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static Api.AspNetCore.Services.IUserManagementService;

namespace Api.AspNetCore.Services
{
    public class UserManagementService : IUserManagementService
    {
        public virtual async Task<IEnumerable<UserRole>> GetRoles(string login)
        {
            return new List<UserRole> { new UserRole { Name = "anonymous" } };
        }

        public virtual async Task<User> GetUserByLogin(string login)
        {
            return new User { Login = login };
        }

        public virtual async Task<IsValidResult> IsValidUser(string username, string password, List<string> allowedRoles = null)
        {
            return IsValidResult.Success;
        }
        public virtual async Task SetRefreshToken(string username, RefreshToken refreshToken)
        {

        }

        public virtual async Task<JwtToken> CheckAttempts(string username, string isValidMessage)
        {
            return null;
        }

        public virtual async Task<IsValidRefreshTokenResult> IsValidRefreshToken(string username, string refreshToken)
        {
            return IsValidRefreshTokenResult.Success;
        }

        public virtual async Task<RefreshToken> GetRefreshToken(string username)
        {
            return null;
        }
    }
}
