using System;
using System.Threading.Tasks;
using Api.AspNetCore.Models.Secure;
using Api.AspNetCore.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.AspNetCore.Controllers
{
    public abstract class AuthenticateControllerBase2<TTokenRequest> : ControllerBase
        where TTokenRequest : JwtTokenRequest
    {
        private readonly IAuthenticateService authService;
        public AuthenticateControllerBase2(IAuthenticateService authService)
        {
            this.authService = authService;
        }

        public virtual async Task<ActionResult> RequestTokenAsync(TTokenRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var token = await authService.IsAuthenticated(request);
            if (token != null)
            {
                if (!string.IsNullOrEmpty(token.ErrorCode))
                    return Unauthorized(token.ErrorCode);

                return Ok(token);
            }

            return BadRequest("Invalid Request");
        }

        protected async Task<ActionResult> GenerateRefreshTokenAsync(string userName)
        {
            var token = await authService.GenerateRefreshTokenAsync(userName);
            if (token == null)
                return Unauthorized("Access denied");
            return Ok(token);
        }

        public virtual async Task<ActionResult> RequestRefreshTokenAsync(RefreshTokenRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var token = await authService.RefreshTokenAsync(request);
            if (token == null)
                return Unauthorized("Access denied");
            if (!string.IsNullOrEmpty(token.ErrorCode))
                return Unauthorized(token.ErrorCode);

            return Ok(token);
        }
    }
}
