using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceDb.Models;
using SpaceDb.Services;

namespace SpaceDb.Controllers;

/// <summary>
/// Points Controller for managing knowledge points
/// </summary>
[Route("/api/v1/points")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PointsController : ControllerBase
{
    private readonly ISpaceDbService _spaceDbService;
    private readonly ILogger<PointsController> _logger;

    public PointsController(
        ISpaceDbService spaceDbService,
        ILogger<PointsController> logger)
    {
        _spaceDbService = spaceDbService ?? throw new ArgumentNullException(nameof(spaceDbService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Add a point to RocksDB and Qdrant
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<long?>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddPoint([FromBody] AddPointRequest request)
    {
        try
        {
            var point = new Point
            {
                Id = request.Id ?? 0,
                Layer = request.Layer,
                Dimension = request.Dimension,
                Weight = request.Weight,
                SingularityId = request.SingularityId,
                UserId = request.UserId,
                Payload = request.Payload
            };

            var pointId = await _spaceDbService.AddPointAsync(request.FromId, point);
            
            if (pointId.HasValue)
            {
                return Ok(new ApiResponse<long?>(pointId, $"Point {pointId} added successfully"));
            }
            else
            {
                return BadRequest(new ApiResponse<long?>(null, "Failed to add point"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding point");
            return StatusCode(500, new ApiResponse<long?>(null, "Internal server error"));
        }
    }

    /// <summary>
    /// Search points in Qdrant
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromBody] SearchRequest request)
    {
        try
        {
            if (request == null || (string.IsNullOrEmpty(request.Query) && request.QueryEmbedding?.Vector == null))
            {
                return BadRequest(new ApiResponse<IEnumerable<object>>(Enumerable.Empty<object>(), "Query or QueryEmbedding is required"));
            }

            IEnumerable<object> results;
            
            if (!string.IsNullOrEmpty(request.Query))
            {
                results = await _spaceDbService.SearchAsync(
                    query: request.Query,
                    singularityId: request.SingularityId,
                    dimension: request.Dimension,
                    layer: request.Layer,
                    limit: request.Limit ?? 10,
                    scoreThreshold: request.ScoreThreshold ?? 0.0f);
            }
            else
            {
                results = await _spaceDbService.SearchWithEmbeddingAsync(
                    queryEmbedding: request.QueryEmbedding!,
                    singularityId: request.SingularityId,
                    dimension: request.Dimension,
                    layer: request.Layer,
                    limit: request.Limit ?? 10,
                    scoreThreshold: request.ScoreThreshold ?? 0.0f);
            }

            return Ok(new ApiResponse<IEnumerable<object>>(results, $"Found {results.Count()} results"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during search");
            return StatusCode(500, new ApiResponse<IEnumerable<object>>(Enumerable.Empty<object>(), "Internal server error"));
        }
    }

    /// <summary>
    /// Update a point in RocksDB and Qdrant
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePoint([FromBody] UpdatePointRequest request)
    {
        try
        {
            var point = new Point
            {
                Id = request.Id,
                Layer = request.Layer,
                Dimension = request.Dimension,
                Weight = request.Weight,
                SingularityId = request.SingularityId,
                UserId = request.UserId,
                Payload = request.Payload
            };

            bool updated;
            if (request.QueryEmbedding?.Vector != null && request.QueryEmbedding.Vector.Count > 0)
            {
                updated = await _spaceDbService.UpdatePointAsync(point, request.QueryEmbedding);
            }
            else
            {
                updated = await _spaceDbService.UpdatePointAsync(point);
            }

            if (updated)
            {
                return Ok(new ApiResponse<bool>(updated, $"Point {request.Id} updated successfully"));
            }
            else
            {
                return BadRequest(new ApiResponse<bool>(false, "Failed to update point"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating point");
            return StatusCode(500, new ApiResponse<bool>(false, "Internal server error"));
        }
    }

    /// <summary>
    /// Delete a point from RocksDB and Qdrant
    /// </summary>
    [HttpDelete("{pointId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeletePoint(long pointId)
    {
        try
        {
            var deleted = await _spaceDbService.DeletePointAsync(pointId);
            
            if (deleted)
            {
                return Ok(new ApiResponse<bool>(true, $"Point {pointId} deleted successfully"));
            }
            else
            {
                return NotFound(new ApiResponse<bool>(false, $"Point {pointId} not found"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting point");
            return StatusCode(500, new ApiResponse<bool>(false, "Internal server error"));
        }
    }
}

public class AddPointRequest
{
    public long? FromId { get; set; }
    public long? Id { get; set; }
    public int Layer { get; set; }
    public int Dimension { get; set; }
    public double Weight { get; set; }
    public long? SingularityId { get; set; }
    public int? UserId { get; set; }
    public string? Payload { get; set; }
}

public class UpdatePointRequest
{
    public long Id { get; set; }
    public int Layer { get; set; }
    public int Dimension { get; set; }
    public double Weight { get; set; }
    public long? SingularityId { get; set; }
    public int? UserId { get; set; }
    public string? Payload { get; set; }
    public AI.AIEmbedding? QueryEmbedding { get; set; }
}

public class SearchRequest
{
    public string? Query { get; set; }
    public AI.AIEmbedding? QueryEmbedding { get; set; }
    public long? SingularityId { get; set; }
    public int? Dimension { get; set; }
    public int? Layer { get; set; }
    public uint? Limit { get; set; }
    public float? ScoreThreshold { get; set; }
}

