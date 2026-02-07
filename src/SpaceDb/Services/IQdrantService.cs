using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace SpaceDb.Services;

public interface IQdrantService
{
    Task<bool> CreateCollectionAsync(string collectionName, uint vectorSize, string distance = "cosine");
    Task<bool> DeleteCollectionAsync(string collectionName);
    Task<bool> CollectionExistsAsync(string collectionName);
    Task<IEnumerable<string>> GetCollectionsAsync();
    Task<bool> UpsertPointsAsync(string collectionName, IEnumerable<object> points);
    Task<bool> UpsertPointStructsAsync(string collectionName, IEnumerable<PointStruct> points);
    Task<bool> UpsertPointsFromTextAsync(string collectionName, string embeddingType, IEnumerable<(string pointId, string text, object? label, Dictionary<string, object>? payload)> points, CancellationToken cancellationToken = default);
    Task<bool> DeletePointsAsync(string collectionName, IEnumerable<string> pointIds);
    Task<IEnumerable<object>> SearchAsync(string collectionName, float[] vector, uint limit = 10, float scoreThreshold = 0.0f);
    Task<IEnumerable<object>> SearchWithFilterAsync(string collectionName, float[] vector, Dictionary<string, object>? filter = null, uint limit = 10, float scoreThreshold = 0.0f);
    Task<IEnumerable<object>> SearchByTextAsync(string collectionName, string embeddingType, string queryText, uint limit = 10, float scoreThreshold = 0.0f, CancellationToken cancellationToken = default);
    Task<bool> CreatePayloadIndexAsync(string collectionName, string fieldName, string schemaType);
    Task<bool> DeletePayloadIndexAsync(string collectionName, string fieldName);
    Task<IEnumerable<object>> GetPayloadIndexesAsync(string collectionName);
    Task<bool> SetPayloadAsync(string collectionName, string pointId, Dictionary<string, object> payload);
    Task<bool> DeletePayloadAsync(string collectionName, string pointId, IEnumerable<string> keys);
    Task<Dictionary<string, object>?> GetPayloadAsync(string collectionName, string pointId);
    Task<object?> GetCollectionInfoAsync(string collectionName);
}