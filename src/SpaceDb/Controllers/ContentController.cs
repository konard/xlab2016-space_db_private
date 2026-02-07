using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceDb.Services;

namespace SpaceDb.Controllers;

/// <summary>
/// Content Controller for uploading and parsing resources
/// Creates hierarchical structure: Resource (dimension=0) -> Contents (dimension=1)
/// </summary>
[Route("/api/v1/content")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ContentController : ControllerBase
{
    private readonly IContentParserService _contentParserService;
    private readonly ILogger<ContentController> _logger;

    public ContentController(
        IContentParserService contentParserService,
        ILogger<ContentController> logger)
    {
        _contentParserService = contentParserService ?? throw new ArgumentNullException(nameof(contentParserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Upload and parse content into resource with fragments
    /// </summary>
    /// <remarks>
    /// Creates a hierarchical structure:
    /// - Resource point at dimension=0 (represents the entire document/file)
    /// - Fragment points at dimension=1 (paragraphs for text, nodes for JSON)
    /// - Segments connecting resource to fragments
    ///
    /// Content types:
    /// - "text": Splits into paragraphs
    /// - "json": Converts to graph structure
    /// - "auto": Auto-detects content type
    /// </remarks>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ApiResponse<ContentParseResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadContent([FromBody] UploadContentRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Payload))
            {
                return BadRequest(new ApiResponse<ContentParseResult>(null, "Payload is required"));
            }

            if (string.IsNullOrWhiteSpace(request.ResourceId))
            {
                return BadRequest(new ApiResponse<ContentParseResult>(null, "ResourceId is required"));
            }

            _logger.LogInformation("Uploading content for resource {ResourceId}, type: {ContentType}",
                request.ResourceId, request.ContentType ?? "auto");

            var result = await _contentParserService.ParseAndStoreAsync(
                payload: request.Payload,
                resourceId: request.ResourceId,
                contentType: request.ContentType ?? "auto",
                singularityId: request.SingularityId,
                userId: request.UserId,
                metadata: request.Metadata);

            if (result == null)
            {
                return BadRequest(new ApiResponse<ContentParseResult>(
                    null,
                    "Failed to parse and store content"));
            }

            return Ok(new ApiResponse<ContentParseResult>(
                result,
                $"Successfully created resource {result.ResourcePointId} with {result.TotalFragments} fragments"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading content for resource {ResourceId}", request.ResourceId);
            return StatusCode(500, new ApiResponse<ContentParseResult>(
                null,
                "Internal server error"));
        }
    }

    /// <summary>
    /// Get available parser types
    /// </summary>
    [HttpGet("parsers")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<string>>), StatusCodes.Status200OK)]
    public IActionResult GetParsers()
    {
        var parsers = _contentParserService.GetAvailableParserTypes();
        return Ok(new ApiResponse<IEnumerable<string>>(
            parsers,
            $"Available parsers: {string.Join(", ", parsers)}"));
    }

    /// <summary>
    /// Upload OpenCyc OWL/RDF ontology
    /// </summary>
    /// <remarks>
    /// Specialized endpoint for OpenCyc OWL/RDF format.
    /// Parses ontology into resource with fragments for:
    /// - Ontology header
    /// - Classes (with subClassOf, sameAs relationships)
    /// - Properties (ObjectProperty, DatatypeProperty, etc.)
    /// - Individuals (NamedIndividual instances)
    /// </remarks>
    [HttpPost("opencyc/owl")]
    [ProducesResponseType(typeof(ApiResponse<ContentParseResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadOpenCycOwl([FromBody] UploadOwlRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Payload))
            {
                return BadRequest(new ApiResponse<ContentParseResult>(null, "OWL/RDF payload is required"));
            }

            if (string.IsNullOrWhiteSpace(request.ResourceId))
            {
                return BadRequest(new ApiResponse<ContentParseResult>(null, "ResourceId is required"));
            }

            _logger.LogInformation("Uploading OpenCyc OWL/RDF for resource {ResourceId}", request.ResourceId);

            // Force content type to "owl"
            var result = await _contentParserService.ParseAndStoreAsync(
                payload: request.Payload,
                resourceId: request.ResourceId,
                contentType: "owl",
                singularityId: request.SingularityId,
                userId: request.UserId,
                metadata: request.Metadata);

            if (result == null)
            {
                return BadRequest(new ApiResponse<ContentParseResult>(
                    null,
                    "Failed to parse and store OWL/RDF content"));
            }

            return Ok(new ApiResponse<ContentParseResult>(
                result,
                $"Successfully created resource {result.ResourcePointId} with {result.TotalFragments} OWL fragments"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading OpenCyc OWL for resource {ResourceId}", request.ResourceId);
            return StatusCode(500, new ApiResponse<ContentParseResult>(
                null,
                "Internal server error"));
        }
    }

    /// <summary>
    /// Upload multiple content items in batch
    /// </summary>
    [HttpPost("upload/batch")]
    [ProducesResponseType(typeof(ApiResponse<List<ContentParseResult>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadContentBatch([FromBody] BatchUploadContentRequest request)
    {
        try
        {
            if (request.Items == null || request.Items.Count == 0)
            {
                return BadRequest(new ApiResponse<List<ContentParseResult>>(
                    null,
                    "At least one item is required"));
            }

            _logger.LogInformation("Batch uploading {Count} content items", request.Items.Count);

            var results = new List<ContentParseResult>();

            foreach (var item in request.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Payload) || string.IsNullOrWhiteSpace(item.ResourceId))
                {
                    _logger.LogWarning("Skipping item with empty payload or resourceId");
                    continue;
                }

                var result = await _contentParserService.ParseAndStoreAsync(
                    payload: item.Payload,
                    resourceId: item.ResourceId,
                    contentType: item.ContentType ?? "auto",
                    singularityId: request.SingularityId ?? item.SingularityId,
                    userId: request.UserId ?? item.UserId,
                    metadata: item.Metadata);

                if (result != null)
                {
                    results.Add(result);
                }
                else
                {
                    _logger.LogWarning("Failed to parse item {ResourceId}", item.ResourceId);
                }
            }

            var totalFragments = results.Sum(r => r.TotalFragments);

            return Ok(new ApiResponse<List<ContentParseResult>>(
                results,
                $"Successfully processed {results.Count}/{request.Items.Count} items with {totalFragments} total fragments"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch upload");
            return StatusCode(500, new ApiResponse<List<ContentParseResult>>(
                null,
                "Internal server error"));
        }
    }
}

/// <summary>
/// Request for uploading content
/// </summary>
public class UploadContentRequest
{
    /// <summary>
    /// Raw content payload
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Resource identifier (filename, URL, etc.)
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// Content type: "text", "json", or "auto"
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Singularity namespace
    /// </summary>
    public long? SingularityId { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Request for batch upload
/// </summary>
public class BatchUploadContentRequest
{
    /// <summary>
    /// Content items to upload
    /// </summary>
    public List<UploadContentRequest> Items { get; set; } = new();

    /// <summary>
    /// Default singularity ID for all items (can be overridden per item)
    /// </summary>
    public long? SingularityId { get; set; }

    /// <summary>
    /// Default user ID for all items (can be overridden per item)
    /// </summary>
    public int? UserId { get; set; }
}

/// <summary>
/// Request for uploading OpenCyc OWL/RDF content
/// </summary>
public class UploadOwlRequest
{
    /// <summary>
    /// OWL/RDF XML payload
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Resource identifier (filename, URL, etc.)
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// Singularity namespace
    /// </summary>
    public long? SingularityId { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
