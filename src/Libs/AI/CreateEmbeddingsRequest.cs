namespace AI
{
    public class CreateEmbeddingsRequest
    {
        public string ApiToken { get; set; }

        public string Type { get; set; } = string.Empty;
        public List<EmbeddingValue> Values { get; set; } = new();

        public bool ReturnVectors { get; set; } = false;

        public class EmbeddingValue
        {
            public object Label { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
        }
    }
}
