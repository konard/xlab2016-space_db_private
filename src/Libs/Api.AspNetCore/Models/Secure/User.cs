using System.Collections.Generic;

namespace Api.AspNetCore.Models.Secure
{
    public class User
    {
        public string Id { get; set; }
        public string Login { get; set; }

        public List<UserRole> Roles { get; set; }
    }
}
