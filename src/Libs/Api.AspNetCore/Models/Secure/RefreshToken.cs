using System;
using System.Collections.Generic;
using System.Text;

namespace Api.AspNetCore.Models.Secure
{
    public class RefreshToken
    {
        public string Token { get; set; }
        public DateTime Expires { get; set; }
        public DateTime Created { get; set; }
    }
}
