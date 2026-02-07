namespace AI
{
    public class AIEmbedding
    {
        public long Id { get; set; }
        public string? Label { get; set; }
        public List<float>? Vector { get; set; }
        public double Similarity { get; set; }
        /// <summary>
        /// Токены
        /// </summary>
        public int? TotalTokens { get; set; }
    }
}
