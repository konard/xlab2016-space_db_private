using Api.AspNetCore.Models.Secure;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Api.AspNetCore.Services
{
    public interface IAuthenticateService
    {
        Task<JwtToken> IsAuthenticated(JwtTokenRequest request,
            Func<User, List<Claim>, Task> claimOperation = null);
        Task<RefreshToken> GenerateRefreshTokenAsync(string userName);
        Task<JwtToken> RefreshTokenAsync(RefreshTokenRequest request, Func<User, List<Claim>, Task> claimOperation = null);
    }
}
