using Data.Repository;

namespace SpaceDb.Data.SpaceDb.Entities
{
    /// <summary>
    /// Сингулярность
    /// </summary>
    public partial class Singularity : IEntityKey<long>
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public int Version { get; set; }
        public bool Private { get; set; }
    }
}
