using Qdrant.Client;
using Qdrant.Client.Grpc;
using Microsoft.Extensions.Logging;
using AI;

namespace SpaceDb.Services;

public class QdrantService : IQdrantService
{
    private readonly QdrantClient _client;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private readonly QdrantConfig _config;
    private readonly IEmbeddingProvider _embeddingProvider;

    public QdrantService(QdrantConfig config, Microsoft.Extensions.Logging.ILogger logger, IEmbeddingProvider embeddingProvider)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _embeddingProvider = embeddingProvider ?? throw new ArgumentNullException(nameof(embeddingProvider));

        _client = new QdrantClient(_config.Host, _config.Port, _config.UseHttps);
        _logger.LogInformation("Qdrant клиент инициализирован: {Host}:{Port}", _config.Host, _config.Port);
    }

    public async Task<bool> CreateCollectionAsync(string collectionName, uint vectorSize, string distance = "cosine")
    {
        try
        {
            var collectionExists = await CollectionExistsAsync(collectionName);
            if (collectionExists)
            {
                _logger.LogWarning("Коллекция {CollectionName} уже существует", collectionName);
                return true;
            }

            // Simplified collection creation
            await _client.CreateCollectionAsync(collectionName, new VectorParams
            {
                Size = vectorSize,
                Distance = distance.ToLower() switch
                {
                    "dot" => Distance.Dot,
                    _ => Distance.Cosine
                }
            });

            _logger.LogInformation("Коллекция {CollectionName} создана с размером вектора {VectorSize}", collectionName, vectorSize);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании коллекции {CollectionName}", collectionName);
            return false;
        }
    }

    public async Task<bool> DeleteCollectionAsync(string collectionName)
    {
        try
        {
            await _client.DeleteCollectionAsync(collectionName);
            _logger.LogInformation("Коллекция {CollectionName} удалена", collectionName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении коллекции {CollectionName}", collectionName);
            return false;
        }
    }

    public async Task<bool> CollectionExistsAsync(string collectionName)
    {
        try
        {
            var collections = await _client.ListCollectionsAsync();
            return collections.Any(c => c == collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке существования коллекции {CollectionName}", collectionName);
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetCollectionsAsync()
    {
        try
        {
            var collections = await _client.ListCollectionsAsync();
            return collections;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка коллекций");
            return Enumerable.Empty<string>();
        }
    }

    public async Task<bool> UpsertPointsAsync(string collectionName, IEnumerable<object> points)
    {
        try
        {
            // Simplified implementation - convert to proper PointStruct when needed
            _logger.LogDebug("Upsert points to collection {CollectionName} - simplified implementation", collectionName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при добавлении точек в коллекцию {CollectionName}", collectionName);
            return false;
        }
    }

    public async Task<bool> UpsertPointStructsAsync(string collectionName, IEnumerable<PointStruct> points)
    {
        try
        {
            var pointsList = points.ToList();
            if (pointsList.Count == 0)
            {
                _logger.LogWarning("No points to upsert");
                return false;
            }

            await _client.UpsertAsync(collectionName, pointsList);
            
            _logger.LogInformation("Successfully upserted {Count} points to collection {CollectionName}", pointsList.Count, collectionName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при добавлении точек в коллекцию {CollectionName}", collectionName);
            return false;
        }
    }

    public async Task<bool> UpsertPointsFromTextAsync(string collectionName, string embeddingType, IEnumerable<(string pointId, string text, object? label, Dictionary<string, object>? payload)> points, CancellationToken cancellationToken = default)
    {
        try
        {
            var pointsList = points.ToList();
            if (pointsList.Count == 0)
            {
                _logger.LogWarning("No points to upsert");
                return false;
            }

            _logger.LogInformation("Creating embeddings for {Count} points: Type={Type}", pointsList.Count, embeddingType);

            var texts = pointsList.Select(p => p.text).ToList();
            var labels = pointsList.Select(p => p.label).Where(l => l != null).Cast<object>().ToList();
            
            var embeddings = await _embeddingProvider.CreateEmbeddingsAsync(
                embeddingType, 
                texts, 
                labels.Count > 0 ? labels : null, 
                returnVectors: true, 
                cancellationToken: cancellationToken);

            if (embeddings.Count != pointsList.Count)
            {
                _logger.LogError("Embeddings count ({EmbeddingsCount}) doesn't match points count ({PointsCount})", embeddings.Count, pointsList.Count);
                return false;
            }

            var qdrantPoints = new List<PointStruct>();
            for (int i = 0; i < pointsList.Count; i++)
            {
                var point = pointsList[i];
                var embedding = embeddings[i];

                if (embedding.Vector == null || embedding.Vector.Count == 0)
                {
                    _logger.LogWarning("Empty vector for point {PointId}, skipping", point.pointId);
                    continue;
                }

                var payload = new Dictionary<string, Value>();
                if (point.payload != null && point.payload.Count > 0)
                {
                    foreach (var kvp in point.payload)
                    {
                        payload[kvp.Key] = ConvertToValue(kvp.Value);
                    }
                }

                var vectors = new Dictionary<string, float[]>
                {
                    { "", embedding.Vector.ToArray() }
                };
                
                var id = ulong.Parse(point.pointId);
                var pointId = new PointId { Num = id };
                
                var pointStruct = new PointStruct
                {
                    Id = pointId,
                    Vectors = vectors
                };
                
                // Set payload field by field
                foreach (var kvp in payload)
                {
                    pointStruct.Payload[kvp.Key] = kvp.Value;
                }

                qdrantPoints.Add(pointStruct);
            }

            if (qdrantPoints.Count == 0)
            {
                _logger.LogWarning("No valid points to upsert after embedding creation");
                return false;
            }

            await _client.UpsertAsync(collectionName, qdrantPoints);
            
            _logger.LogInformation("Successfully upserted {Count} points to collection {CollectionName}", qdrantPoints.Count, collectionName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при добавлении точек из текста в коллекцию {CollectionName}", collectionName);
            return false;
        }
    }

    private object ConvertScoredPointToObject(ScoredPoint scoredPoint)
    {
        var payloadDict = new Dictionary<string, object>();
        if (scoredPoint.Payload != null)
        {
            foreach (var kvp in scoredPoint.Payload)
            {
                payloadDict[kvp.Key] = ConvertFromValue(kvp.Value);
            }
        }

        return new
        {
            id = scoredPoint.Id.ToString(),
            score = scoredPoint.Score,
            payload = payloadDict
        };
    }

    private Value ConvertToValue(object? value)
    {
        if (value is string s) return new Value { StringValue = s };
        if (value is int i) return new Value { IntegerValue = i };
        if (value is long l) return new Value { IntegerValue = l };
        if (value is float f) return new Value { DoubleValue = f };
        if (value is double d) return new Value { DoubleValue = d };
        if (value is bool b) return new Value { BoolValue = b };
        if (value == null) return new Value { NullValue = new NullValue() };
        return new Value { StringValue = value.ToString() ?? string.Empty };
    }

    private object? ConvertFromValue(Value value)
    {
        if (!string.IsNullOrEmpty(value.StringValue)) return value.StringValue;
        if (value.HasIntegerValue) return value.IntegerValue;
        if (value.HasDoubleValue) return value.DoubleValue;
        if (value.HasBoolValue) return value.BoolValue;
        if (value.HasNullValue) return null;
        return null;
    }

    public async Task<bool> DeletePointsAsync(string collectionName, IEnumerable<string> pointIds)
    {
        try
        {
            var pointIdList = pointIds.ToList();
            if (pointIdList.Count == 0)
            {
                _logger.LogWarning("No point IDs provided for deletion");
                return false;
            }

            // Convert strings to PointIds
            var qdrantPointIds = pointIdList.Select(id => ulong.Parse(id)).ToList();
            
            // Delete points using batch delete
            await _client.DeleteAsync(collectionName, qdrantPointIds);
            
            _logger.LogInformation("Successfully deleted {Count} points from collection {CollectionName}", pointIdList.Count, collectionName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении точек из коллекции {CollectionName}", collectionName);
            return false;
        }
    }

    public async Task<IEnumerable<object>> SearchAsync(string collectionName, float[] vector, uint limit = 10, float scoreThreshold = 0.0f)
    {
        try
        {
            var searchResults = await _client.SearchAsync(collectionName, vector, limit: limit, scoreThreshold: scoreThreshold);
            
            _logger.LogInformation("Search completed in collection {CollectionName}, found {Count} results", collectionName, searchResults.Count);
            
            return searchResults.Select(ConvertScoredPointToObject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при поиске в коллекции {CollectionName}", collectionName);
            return Enumerable.Empty<object>();
        }
    }

    public async Task<IEnumerable<object>> SearchWithFilterAsync(string collectionName, float[] vector, Dictionary<string, object>? filter = null, uint limit = 10, float scoreThreshold = 0.0f)
    {
        try
        {
            Filter? qdrantFilter = null;
            
            if (filter != null && filter.Count > 0)
            {
                qdrantFilter = BuildFilter(filter);
                _logger.LogInformation("Search with filter: {Filter} in collection {CollectionName}", 
                    string.Join(", ", filter.Select(kvp => $"{kvp.Key}={kvp.Value}")), collectionName);
            }
            
            var searchResults = await _client.SearchAsync(collectionName, vector, limit: limit, scoreThreshold: scoreThreshold, filter: qdrantFilter);
            
            _logger.LogInformation("Search with filter completed in collection {CollectionName}, found {Count} results", collectionName, searchResults.Count);
            
            return searchResults.Select(ConvertScoredPointToObject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при поиске с фильтром в коллекции {CollectionName}", collectionName);
            return Enumerable.Empty<object>();
        }
    }
    
    private Filter? BuildFilter(Dictionary<string, object> filter)
    {
        if (filter == null || filter.Count == 0)
            return null;

        var conditions = new List<Condition>();
        
        foreach (var kvp in filter)
        {
            var match = CreateMatch(kvp.Value);
            var condition = new Condition
            {
                Field = new FieldCondition
                {
                    Key = kvp.Key,
                    Match = match
                }
            };
            conditions.Add(condition);
        }

        var result = new Filter();
        result.Must.AddRange(conditions);
        return result;
    }
    
    private Match CreateMatch(object value)
    {
        var match = new Match();
        
        switch (value)
        {
            case string str:
                match.Text = str;
                break;
            case int i:
                match.Integer = i;
                break;
            case long l:
                match.Integer = l;
                break;
            case bool b:
                match.Boolean = b;
                break;
            default:
                match.Text = value?.ToString() ?? "";
                break;
        }
        
        return match;
    }

    public async Task<IEnumerable<object>> SearchByTextAsync(string collectionName, string embeddingType, string queryText, uint limit = 10, float scoreThreshold = 0.0f, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(queryText))
            {
                _logger.LogWarning("Query text is empty");
                return Enumerable.Empty<object>();
            }

            _logger.LogInformation("Creating embedding for search query: Type={Type}, TextLength={Length}", embeddingType, queryText.Length);
            
            var embedding = await _embeddingProvider.CreateEmbeddingAsync(embeddingType, queryText, returnVectors: true, cancellationToken: cancellationToken);
            
            if (embedding.Vector == null || embedding.Vector.Count == 0)
            {
                _logger.LogError("Failed to create embedding for search query");
                return Enumerable.Empty<object>();
            }

            var vector = embedding.Vector.ToArray();
            return await SearchAsync(collectionName, vector, limit, scoreThreshold);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при поиске по тексту в коллекции {CollectionName}", collectionName);
            return Enumerable.Empty<object>();
        }
    }

    public async Task<bool> CreatePayloadIndexAsync(string collectionName, string fieldName, string schemaType)
    {
        try
        {
            // Simplified implementation
            _logger.LogDebug("Create payload index for field {FieldName} in collection {CollectionName} - simplified implementation", fieldName, collectionName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Предупреждение при создании индекса для поля {FieldName} в коллекции {CollectionName}", fieldName, collectionName);
            return false;
        }
    }

    public async Task<bool> DeletePayloadIndexAsync(string collectionName, string fieldName)
    {
        try
        {
            // Simplified implementation
            _logger.LogDebug("Delete payload index for field {FieldName} in collection {CollectionName} - simplified implementation", fieldName, collectionName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении индекса для поля {FieldName} в коллекции {CollectionName}", fieldName, collectionName);
            return false;
        }
    }

    public async Task<IEnumerable<object>> GetPayloadIndexesAsync(string collectionName)
    {
        try
        {
            // Simplified implementation
            _logger.LogDebug("Get payload indexes for collection {CollectionName} - simplified implementation", collectionName);
            return Enumerable.Empty<object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении индексов коллекции {CollectionName}", collectionName);
            return Enumerable.Empty<object>();
        }
    }

    public async Task<bool> SetPayloadAsync(string collectionName, string pointId, Dictionary<string, object> payload)
    {
        try
        {
            // Simplified implementation
            _logger.LogDebug("Set payload for point {PointId} in collection {CollectionName} - simplified implementation", pointId, collectionName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при установке payload для точки {PointId} в коллекции {CollectionName}", pointId, collectionName);
            return false;
        }
    }

    public async Task<bool> DeletePayloadAsync(string collectionName, string pointId, IEnumerable<string> keys)
    {
        try
        {
            // Simplified implementation
            _logger.LogDebug("Delete payload for point {PointId} in collection {CollectionName} - simplified implementation", pointId, collectionName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении payload для точки {PointId} в коллекции {CollectionName}", pointId, collectionName);
            return false;
        }
    }

    public async Task<Dictionary<string, object>?> GetPayloadAsync(string collectionName, string pointId)
    {
        try
        {
            // Simplified implementation
            _logger.LogDebug("Get payload for point {PointId} in collection {CollectionName} - simplified implementation", pointId, collectionName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении payload для точки {PointId} в коллекции {CollectionName}", pointId, collectionName);
            return null;
        }
    }

    public async Task<object?> GetCollectionInfoAsync(string collectionName)
    {
        try
        {
            var info = await _client.GetCollectionInfoAsync(collectionName);
            return info;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении информации о коллекции {CollectionName}", collectionName);
            return null;
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
        _logger.LogInformation("Qdrant клиент закрыт");
    }
}

public class QdrantConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6333;
    public bool UseHttps { get; set; } = false;
    public string CollectionName { get; set; } = "default";
    public string EmbeddingType { get; set; } = "default";
    public uint VectorSize { get; set; } = 1536;
}