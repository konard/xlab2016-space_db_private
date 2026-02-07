using Data.Repository;
using SpaceDb.Data.SpaceDb.Entities;

namespace SpaceDb.Models.Queries.UserRoles
{
    /// <summary>
    /// Присвоенная роль
    /// </summary>
    public partial class UserRoleFilter : FilterBase<UserRole>
    {
        public FilterOperand<int>? Id { get; set; }
        public FilterOperand<int?>? UserId { get; set; }
        public FilterOperand<int?>? RoleId { get; set; }
    }
}
