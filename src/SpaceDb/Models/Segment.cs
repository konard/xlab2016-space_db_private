using Data.Repository;

namespace SpaceDb.Models
{
    /// <summary>
    /// Отрезок знания
    /// </summary>
    public partial class Segment : IEntityKey<long>
    {
        public long Id { get; set; }
        public double? Weight { get; set; }
        public int Layer { get; set; }
        public int Dimension { get; set; }
        public long? SingularityId { get; set; }
        public long? FromId { get; set; }
        public long? ToId { get; set; }
    }
}
