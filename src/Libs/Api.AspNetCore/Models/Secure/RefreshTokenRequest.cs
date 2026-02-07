using System;
using System.Collections.Generic;
using System.Text;

namespace Api.AspNetCore.Models.Secure
{
    public class RefreshTokenRequest
    {
        public string UserName { get; set; }
        public string RefreshToken { get; set; }
    }
}
