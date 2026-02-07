using AI;

namespace SpaceDb.Services;

public abstract class EmbeddingProvider : IEmbeddingProvider
{
    public virtual async Task<List<AIEmbedding>> CreateEmbeddingsAsync(string type, List<string> texts, List<object>? labels = null, bool returnVectors = false, CancellationToken cancellationToken = default)
    {
        if (texts == null || texts.Count == 0)
        {
            throw new ArgumentException("Texts cannot be null or empty", nameof(texts));
        }

        var embeddingValues = texts.Select((text, index) => new CreateEmbeddingsRequest.EmbeddingValue
        {
            Text = text,
            Label = labels != null && index < labels.Count ? labels[index] : string.Empty
        }).ToList();

        var request = new CreateEmbeddingsRequest
        {
            Type = type,
            Values = embeddingValues,
            ReturnVectors = returnVectors
        };

        return await CreateEmbeddingsInternalAsync(request, cancellationToken);
    }

    public virtual async Task<AIEmbedding> CreateEmbeddingAsync(string type, string text, object? label = null, bool returnVectors = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentException("Text cannot be null or empty", nameof(text));
        }

        var result = await CreateEmbeddingsAsync(type, new List<string> { text }, label != null ? new List<object> { label } : null, returnVectors, cancellationToken);
        
        return result.FirstOrDefault() ?? throw new InvalidOperationException("Failed to create embedding");
    }

    protected abstract Task<List<AIEmbedding>> CreateEmbeddingsInternalAsync(CreateEmbeddingsRequest request, CancellationToken cancellationToken);
}

