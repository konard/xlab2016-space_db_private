using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using SpaceDb.Services;
using System.Text.Json;

namespace SpaceDb.Controllers;

/// <summary>
/// Qdrant Vector Database Controller
/// </summary>
[Route("/api/v1/qdrant")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class QdrantController : ControllerBase
{
    private readonly IQdrantService _qdrantService;
    private readonly IQdrantPayloadIndexService _payloadIndexService;
    private readonly ILogger<QdrantController> _logger;

    public QdrantController(
        IQdrantService qdrantService, 
        IQdrantPayloadIndexService payloadIndexService,
        ILogger<QdrantController> logger)
    {
        _qdrantService = qdrantService ?? throw new ArgumentNullException(nameof(qdrantService));
        _payloadIndexService = payloadIndexService ?? throw new ArgumentNullException(nameof(payloadIndexService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new collection
    /// </summary>
    [HttpPost("collections")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateCollection([FromBody] CreateCollectionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CollectionName) || request.VectorSize == 0)
            {
                return BadRequest(new ApiResponse<bool>(false, "Collection name and vector size are required"));
            }

            var result = await _qdrantService.CreateCollectionAsync(request.CollectionName, request.VectorSize, request.Distance ?? "cosine");
            
            if (result && request.CreatePayloadIndexes)
            {
                await _payloadIndexService.CreatePayloadIndexesAsync(request.CollectionName);
            }

            return Ok(new ApiResponse<bool>(result, result ? "Collection created successfully" : "Failed to create collection"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating collection: {CollectionName}", request.CollectionName);
            return StatusCode(500, new ApiResponse<bool>(false, "Internal server error"));
        }
    }

    /// <summary>
    /// Delete a collection
    /// </summary>
    [HttpDelete("collections/{collectionName}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteCollection(string collectionName)
    {
        try
        {
            var result = await _qdrantService.DeleteCollectionAsync(collectionName);
            return Ok(new ApiResponse<bool>(result, result ? "Collection deleted successfully" : "Failed to delete collection"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting collection: {CollectionName}", collectionName);
            return StatusCode(500, new ApiResponse<bool>(false, "Internal server error"));
        }
    }

    /// <summary>
    /// Check if collection exists
    /// </summary>
    [HttpGet("collections/{collectionName}/exists")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CollectionExists(string collectionName)
    {
        try
        {
            var exists = await _qdrantService.CollectionExistsAsync(collectionName);
            return Ok(new ApiResponse<bool>(exists, exists ? "Collection exists" : "Collection does not exist"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking collection existence: {CollectionName}", collectionName);
            return StatusCode(500, new ApiResponse<bool>(false, "Internal server error"));
        }
    }

    /// <summary>
    /// Get all collections
    /// </summary>
    [HttpGet("collections")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<string>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCollections()
    {
        try
        {
            var collections = await _qdrantService.GetCollectionsAsync();
            return Ok(new ApiResponse<IEnumerable<string>>(collections, "Collections retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving collections");
            return StatusCode(500, new ApiResponse<IEnumerable<string>>(null, "Internal server error"));
        }
    }

    /// <summary>
    /// Get collection information
    /// </summary>
    [HttpGet("collections/{collectionName}/info")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCollectionInfo(string collectionName)
    {
        try
        {
            var info = await _qdrantService.GetCollectionInfoAsync(collectionName);
            if (info == null)
            {
                return NotFound(new ApiResponse<object>(null, "Collection not found"));
            }

            return Ok(new ApiResponse<object>(info, "Collection info retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving collection info: {CollectionName}", collectionName);
            return StatusCode(500, new ApiResponse<object>(null, "Internal server error"));
        }
    }

    /// <summary>
    /// Create default payload indexes
    /// </summary>
    [HttpPost("collections/{collectionName}/indexes/default")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateDefaultPayloadIndexes(string collectionName)
    {
        try
        {
            await _payloadIndexService.CreatePayloadIndexesAsync(collectionName);
            return Ok(new ApiResponse<bool>(true, "Default payload indexes created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating default payload indexes for collection: {CollectionName}", collectionName);
            return StatusCode(500, new ApiResponse<bool>(false, "Internal server error"));
        }
    }

    /// <summary>
    /// Get payload indexes for collection
    /// </summary>
    [HttpGet("collections/{collectionName}/indexes")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayloadIndexes(string collectionName)
    {
        try
        {
            var indexes = await _qdrantService.GetPayloadIndexesAsync(collectionName);
            return Ok(new ApiResponse<IEnumerable<object>>(indexes, "Payload indexes retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payload indexes for collection: {CollectionName}", collectionName);
            return StatusCode(500, new ApiResponse<IEnumerable<object>>(null, "Internal server error"));
        }
    }
}

public class CreateCollectionRequest
{
    public string CollectionName { get; set; } = string.Empty;
    public uint VectorSize { get; set; }
    public string? Distance { get; set; } = "cosine";
    public bool CreatePayloadIndexes { get; set; } = true;
}