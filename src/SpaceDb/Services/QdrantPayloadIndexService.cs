using Microsoft.Extensions.Logging;

namespace SpaceDb.Services;

public interface IQdrantPayloadIndexService
{
    Task CreatePayloadIndexesAsync(string collectionName);
    Task CreateDefaultPayloadIndexesAsync();
}

public class QdrantPayloadIndexService : IQdrantPayloadIndexService
{
    private readonly IQdrantService _qdrantService;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private readonly QdrantConfig _config;

    public QdrantPayloadIndexService(IQdrantService qdrantService, QdrantConfig config, Microsoft.Extensions.Logging.ILogger logger)
    {
        _qdrantService = qdrantService ?? throw new ArgumentNullException(nameof(qdrantService));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task CreatePayloadIndexesAsync(string collectionName)
    {
        try
        {
            _logger.LogInformation("Создание индексов для полей payload в коллекции {CollectionName}", collectionName);

            // Создаем индексы для быстрого фильтрования по полям
            await _qdrantService.CreatePayloadIndexAsync(
                collectionName: collectionName,
                fieldName: "id",
                schemaType: "integer"
            );

            await _qdrantService.CreatePayloadIndexAsync(
                collectionName: collectionName,
                fieldName: "singularityId",
                schemaType: "integer"
            );

            await _qdrantService.CreatePayloadIndexAsync(
                collectionName: collectionName,
                fieldName: "spaceDimension",
                schemaType: "integer"
            );

            await _qdrantService.CreatePayloadIndexAsync(
                collectionName: collectionName,
                fieldName: "layer",
                schemaType: "integer"
            );

            await _qdrantService.CreatePayloadIndexAsync(
                collectionName: collectionName,
                fieldName: "weight",
                schemaType: "float"
            );

            await _qdrantService.CreatePayloadIndexAsync(
                collectionName: collectionName,
                fieldName: "state",
                schemaType: "integer"
            );

            await _qdrantService.CreatePayloadIndexAsync(
                collectionName: collectionName,
                fieldName: "userId",
                schemaType: "integer"
            );

            await _qdrantService.CreatePayloadIndexAsync(
                collectionName: collectionName,
                fieldName: "category",
                schemaType: "integer"
            );

            _logger.LogInformation("Индексы успешно созданы для коллекции {CollectionName}", collectionName);
        }
        catch (Exception ex)
        {
            // Индексы могут уже существовать - это не критично
            _logger.LogWarning(ex, "Предупреждение при создании индексов для коллекции {CollectionName} (возможно они уже существуют)", collectionName);
        }
    }

    public async Task CreateDefaultPayloadIndexesAsync()
    {
        await CreatePayloadIndexesAsync(_config.CollectionName);
    }
}
