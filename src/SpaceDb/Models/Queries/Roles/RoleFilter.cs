using Data.Repository;
using SpaceDb.Data.SpaceDb.Entities;

namespace SpaceDb.Models.Queries.Roles
{
    /// <summary>
    /// Роль
    /// </summary>
    public partial class RoleFilter : FilterBase<Role>
    {
        public FilterOperand<int>? Id { get; set; }
        public FilterOperand<string>? Name { get; set; }
        public FilterOperand<string>? Code { get; set; }
    }
}
