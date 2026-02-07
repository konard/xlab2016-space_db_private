using Data.Repository;
using SpaceDb.Data.SpaceDb.Entities;

namespace SpaceDb.Models.Queries.Singularities
{
    /// <summary>
    /// Сингулярность
    /// </summary>
    public partial class SingularitySort : SortBase<Singularity>
    {
        public SortOperand? Id { get; set; }
        public SortOperand? Name { get; set; }
        public SortOperand? Version { get; set; }
        public SortOperand? Private { get; set; }
    }
}
