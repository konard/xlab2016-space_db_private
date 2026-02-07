using Data.Repository;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpaceDb.Models
{
    /// <summary>
    /// Точка знания
    /// </summary>
    public partial class Point : IEntityKey<long>
    {
        public long Id { get; set; }
        public int Layer { get; set; }
        public int Dimension { get; set; }
        public double Weight { get; set; }
        public long? SingularityId { get; set; }
        public int? UserId { get; set; }

        public string? Payload { get; set; }
    }
}
