
namespace SpaceDb.Models.Dtos
{
    /// <summary>
    /// Присвоенная роль
    /// </summary>
    public partial class UserRoleDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? RoleId { get; set; }

        public UserDto? User { get; set; }
        public RoleDto? Role { get; set; }
    }
}
