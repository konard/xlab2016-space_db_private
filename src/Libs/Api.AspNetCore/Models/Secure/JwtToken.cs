using Data.Repository.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Api.AspNetCore.Models.Secure
{
    /// <summary>
    /// JWT Token response
    /// </summary>
    public class JwtToken
    {
        /// <summary>
        /// JWT Token
        /// </summary>
        [JsonProperty("token")]
        [JsonPropertyName("token")]
        public string Key { get; set; }

        /// <summary>
        /// Refresh Token
        /// </summary>
        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; }

        /// <summary>
        /// JWT Token expiration
        /// </summary>
        [JsonProperty("expires")]
        public DateTime Expires { get; set; }

        /// <summary>
        /// Error if occures
        /// </summary>
        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }

        /// <summary>
        /// Message
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Original user id
        /// </summary>
        [JsonProperty("userId")]
        public string UserId { get; set; }

        /// <summary>
        /// Original user name
        /// </summary>
        [JsonProperty("userName")]
        public string UserName { get; set; }

        /// <summary>
        /// Original roles
        /// </summary>
        [JsonProperty("roles")]
        public List<string> Roles { get; set; }

        public T Extend<T>()
            where T : JwtToken, new()
        {
            return JsonHelper.Extend<JwtToken, T>(this, (source, target) =>
            {
                target.Key = source.Key;
                target.RefreshToken = source.RefreshToken;
                target.ErrorCode = source.ErrorCode;
                target.Expires = source.Expires;
                target.Message = source.Message;
                target.Roles = source.Roles;
                target.UserId = source.UserId;
                target.UserName = source.UserName;
            });
        }
    }
}
