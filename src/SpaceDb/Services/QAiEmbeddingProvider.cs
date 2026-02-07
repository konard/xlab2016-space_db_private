using AI;
using AI.Client;
using Microsoft.Extensions.Logging;

namespace SpaceDb.Services;

public class QAiEmbeddingProvider : EmbeddingProvider
{
    private readonly QAiClient _qAiClient;
    private readonly ILogger<QAiEmbeddingProvider> _logger;

    public QAiEmbeddingProvider(QAiClient qAiClient, ILogger<QAiEmbeddingProvider> logger)
    {
        _qAiClient = qAiClient ?? throw new ArgumentNullException(nameof(qAiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<List<AIEmbedding>> CreateEmbeddingsInternalAsync(CreateEmbeddingsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating embeddings: Type={Type}, Count={Count}, ReturnVectors={ReturnVectors}", 
                request.Type, request.Values.Count, request.ReturnVectors);

            var result = await _qAiClient.CreateEmbeddings(request, cancellationToken);
            
            _logger.LogInformation("Successfully created {Count} embeddings", result.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating embeddings: Type={Type}, Count={Count}", 
                request.Type, request.Values.Count);
            throw;
        }
    }
}

