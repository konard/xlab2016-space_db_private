using Api.AspNetCore.Helpers;
using Api.AspNetCore.Models.Configuration;
using Api.AspNetCore.Models.Secure;
using Api.AspNetCore.Models.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace Api.AspNetCore.Services
{
    public class JwtTokenAuthenticationService : IAuthenticateService
    {
        private readonly TokenManagement tokenManagement;
        private readonly IUserManagementService userManagementService;
        private readonly ILogger<JwtTokenAuthenticationService> logger;

        public JwtTokenAuthenticationService(IUserManagementService userManagementService,
            IOptions<TokenManagement> tokenManagement,
             ILogger<JwtTokenAuthenticationService> logger)
        {
            this.logger = logger;
            this.userManagementService = userManagementService;
            this.tokenManagement = tokenManagement?.Value;
        }

        public virtual async Task<JwtToken> IsAuthenticated(JwtTokenRequest request, 
            Func<User, List<Claim>, Task> claimOperation = null)
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(request.Username))
            {
                logger.LogError($"user name is empty");
                return new JwtToken { ErrorCode = IUserManagementService.IsValidResult.UserNameIsEmpty.ToString() };
            }

            if (string.IsNullOrEmpty(request.Password))
            {
                logger.LogError($"for user {request.Username} password is empty");
                return new JwtToken { ErrorCode = IUserManagementService.IsValidResult.PasswordIsEmpty.ToString() };
            }

            //request.Username = request.Username.ToLower();

            //request.Password.ValidateAsPassword(nameof(request.Password), result);
            //if (result.Errors.Count != 0)
            //{
            //    logger.LogError($"for user {request.Username} passwod language is not english");
            //    return new JwtToken { ErrorCode = IUserManagementService.IsValidResult.LanguageIsNotEnglish.ToString() };
            //}

            var user = await userManagementService.GetUserByLogin(request.Username);

            if (user == null)
            {
                // No user in provider
                logger.LogError($"no user {request.Username} in provider/invalid login or password");
                return new JwtToken { ErrorCode = IUserManagementService.IsValidResult.InvalidLoginOrPassword.ToString() };
            }

            var isValidResult = await userManagementService.IsValidUser(request.Username, request.Password);

            var checkAttemptsResult = await userManagementService.CheckAttempts(request.Username, isValidResult.ToString());

            if (isValidResult != IUserManagementService.IsValidResult.Success)
            {
                // No user in system
                logger.LogError($"no user {request.Username} in system");
                return checkAttemptsResult;
            }

            if (!string.IsNullOrEmpty(checkAttemptsResult.ErrorCode))
                return checkAttemptsResult;
            var roles = (await userManagementService.GetRoles(user.Login)).ToList();
            user.Roles = roles;

            //var refreshToken = GenerateRefreshToken();
            //await userManagementService.SetRefreshToken(user.Login, refreshToken);

            logger.LogInformation($"user {user.Login} logged");

            return await GenerateJwtToken(user, "Use generateRefreshToken service", claimOperation);
        }

        public async Task<RefreshToken> GenerateRefreshTokenAsync(string userName)
        {
            var refreshToken = GenerateRefreshToken();
            await userManagementService.SetRefreshToken(userName, refreshToken);

            return refreshToken;
        }

        public virtual async Task<JwtToken> RefreshTokenAsync(RefreshTokenRequest request, Func<User, List<Claim>, Task> claimOperation = null)
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(request.UserName))
            {
                logger.LogError($"user name is empty");
                return new JwtToken { ErrorCode = IUserManagementService.IsValidResult.UserNameIsEmpty.ToString() };
            }

            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                logger.LogError($"for user {request.RefreshToken} is empty");
                return new JwtToken { ErrorCode = IUserManagementService.IsValidRefreshTokenResult.RefreshTokenIsEmpty.ToString() };
            }

            var user = await userManagementService.GetUserByLogin(request.UserName);

            if (user == null)
            {
                // No user in provider
                logger.LogError($"no user {request.UserName} in provider/invalid login or password");
                return new JwtToken { ErrorCode = IUserManagementService.IsValidResult.InvalidLoginOrPassword.ToString() };
            }

            var isValid = await userManagementService.IsValidRefreshToken(request.UserName, request.RefreshToken);

            if (isValid != IUserManagementService.IsValidRefreshTokenResult.Success)
                return new JwtToken { ErrorCode = "InvalidRefreshToken" };

            var refreshToken = await userManagementService.GetRefreshToken(request.UserName);

            if (refreshToken == null)
                return new JwtToken { ErrorCode = "NotImplemented" };

            if (DateTime.Now >= refreshToken.Expires)
            {
                await userManagementService.SetRefreshToken(user.Login, null);
                return new JwtToken { ErrorCode = "TokenExpired" };
            }

            var roles = (await userManagementService.GetRoles(user.Login)).ToList();
            user.Roles = roles;

            //if (isValid != IUserManagementService.IsValidRefreshTokenResult.Success)
            //{
            //    // No user in system
            //    logger.LogError($"no user {request.UserName} in system");
            //    return await userManagementService.CheckAttempts(request.UserName, isValid.ToString());
            //}

            var newRefreshToken = GenerateRefreshToken();
            await userManagementService.SetRefreshToken(user.Login, newRefreshToken);

            return await GenerateJwtToken(user, newRefreshToken.Token, claimOperation);
        }

        private async Task<JwtToken> GenerateJwtToken(User user, string refreshToken,
            Func<User, List<Claim>, Task> claimOperation = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Login),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
            };

            if (user.Roles != null)
                user.Roles.ForEach(_ =>
                {
                    claims.Add(new Claim(ClaimTypes.Role, _?.Name));
                });

            if (claimOperation != null)
                await claimOperation(user, claims);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenManagement.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            
            var expires = DateTime.Now.AddDays(2);

            var jwtToken = new JwtSecurityToken(
                tokenManagement.Issuer,
                tokenManagement.Audience,
                claims,
                expires: expires,
                signingCredentials: credentials
            );
            // our token
            var token = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            return new JwtToken
            {
                Key = token,
                RefreshToken = refreshToken,
                Expires = expires,
                UserId = user.Id,
                UserName = user.Login,
                Roles = user.Roles != null ? user.Roles.Select(_ => _.Name).ToList() : null
            };
        }

        private RefreshToken GenerateRefreshToken()
        {
            using (var rngCryptoServiceProvider = new RNGCryptoServiceProvider())
            {
                var randomBytes = new byte[64];
                rngCryptoServiceProvider.GetBytes(randomBytes);
                return new RefreshToken
                {
                    Token = Convert.ToBase64String(randomBytes),
                    Expires = DateTime.UtcNow.AddDays(7),
                    Created = DateTime.UtcNow
                };
            }
        }

        public static ClaimsPrincipal TryGetPrincipalFromTokenWithoutKey(string token,
            Action<TokenValidationParameters> configAction = null)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true //here we are saying that we don't care about the token's expiration date
            };

            configAction?.Invoke(tokenValidationParameters);

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");
            return principal;
        }
    }
}
