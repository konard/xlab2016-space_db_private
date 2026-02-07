using Data.Repository;
using SpaceDb.Data.SpaceDb.Entities;

namespace SpaceDb.Models.Queries.UserRoles
{
    /// <summary>
    /// Присвоенная роль
    /// </summary>
    public partial class UserRoleSort : SortBase<UserRole>
    {
        public SortOperand? Id { get; set; }
        public SortOperand? UserId { get; set; }
        public SortOperand? RoleId { get; set; }
    }
}
