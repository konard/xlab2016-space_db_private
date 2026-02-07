using AI;

namespace SpaceDb.Services;

public interface IEmbeddingProvider
{
    Task<List<AIEmbedding>> CreateEmbeddingsAsync(string type, List<string> texts, List<object>? labels = null, bool returnVectors = false, CancellationToken cancellationToken = default);
    Task<AIEmbedding> CreateEmbeddingAsync(string type, string text, object? label = null, bool returnVectors = false, CancellationToken cancellationToken = default);
}

