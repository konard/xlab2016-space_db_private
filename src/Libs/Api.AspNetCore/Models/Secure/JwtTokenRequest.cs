using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Api.AspNetCore.Models.Secure
{
    /// <summary>
    /// JWT Token get request
    /// </summary>
    public class JwtTokenRequest
    {
        /// <summary>
        /// Username
        /// </summary>
        [Required]
        [JsonProperty("username")]
        public string Username { get; set; }


        /// <summary>
        /// Password
        /// </summary>
        [Required]
        [JsonProperty("password")]
        public string Password { get; set; }
    }
}
