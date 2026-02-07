namespace AI
{
    public class SearchEmbeddingsRequest
    {
        public string ApiToken { get; set; }
        public string Type { get; set; }
        public string Query { get; set; } = string.Empty;
        public int K { get; set; }

        public bool ReturnVectors { get; set; } = false;
    }
}
