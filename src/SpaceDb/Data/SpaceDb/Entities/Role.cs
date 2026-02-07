using Data.Repository;

namespace SpaceDb.Data.SpaceDb.Entities
{
    /// <summary>
    /// Роль
    /// </summary>
    public partial class Role : IEntityKey<int>
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }
    }
}
