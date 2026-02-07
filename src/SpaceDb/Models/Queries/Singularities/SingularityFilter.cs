using Data.Repository;
using SpaceDb.Data.SpaceDb.Entities;

namespace SpaceDb.Models.Queries.Singularities
{
    /// <summary>
    /// Сингулярность
    /// </summary>
    public partial class SingularityFilter : FilterBase<Singularity>
    {
        public FilterOperand<long>? Id { get; set; }
        public FilterOperand<string>? Name { get; set; }
        public FilterOperand<int>? Version { get; set; }
        public FilterOperand<bool>? Private { get; set; }
    }
}
