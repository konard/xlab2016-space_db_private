using Api.AspNetCore.Models.Secure;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Api.AspNetCore.Services
{
    public interface IUserManagementService
    {
        Task<IsValidResult> IsValidUser(string username, string password, List<string> allowedRoles = null);
        Task<IsValidRefreshTokenResult> IsValidRefreshToken(string username, string refreshToken);
        Task<RefreshToken> GetRefreshToken(string username);
        Task<IEnumerable<UserRole>> GetRoles(string login);
        Task<User> GetUserByLogin(string login);
        Task SetRefreshToken(string username, RefreshToken refreshToken);
        Task<JwtToken> CheckAttempts(string username, string isValidMessage);

        public enum IsValidResult : int
        {
            Undefined = -1,
            Success = 0,
            RegistrationNotCompleted = 1,
            NotActive = 2,
            InvalidLoginOrPassword = 3,
            UserNameIsEmpty = 4,
            PasswordIsEmpty = 5,
            LanguageIsNotEnglish = 6,
            AttemptsExhausted = 7,
            Error = 8
        }

        public enum IsValidRefreshTokenResult : int
        {
            Undefined = -1,
            Success = 0,
            RefreshTokenIsEmpty = 1,
            InvalidLoginOrPassword = 2,
            NotActive = 3,
            Error = 8
        }
    }
}
