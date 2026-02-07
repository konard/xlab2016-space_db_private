
namespace SpaceDb.Models.Dtos
{
    /// <summary>
    /// Сингулярность
    /// </summary>
    public partial class SingularityDto
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public int Version { get; set; }
        public bool Private { get; set; }
    }
}
