using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceDb.Services;

namespace SpaceDb.Controllers;

/// <summary>
/// Segments Controller for managing knowledge segments
/// </summary>
[Route("/api/v1/segments")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SegmentsController : ControllerBase
{
    private readonly ISpaceDbService _spaceDbService;
    private readonly ILogger<SegmentsController> _logger;

    public SegmentsController(
        ISpaceDbService spaceDbService,
        ILogger<SegmentsController> logger)
    {
        _spaceDbService = spaceDbService ?? throw new ArgumentNullException(nameof(spaceDbService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Add a segment connecting two points
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<long?>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddSegment(
        [FromQuery] long fromId, 
        [FromQuery] long toId)
    {
        try
        {
            var segmentId = await _spaceDbService.AddSegmentAsync(fromId, toId);
            
            if (segmentId.HasValue)
            {
                return Ok(new ApiResponse<long?>(segmentId, $"Segment {segmentId} added successfully"));
            }
            else
            {
                return BadRequest(new ApiResponse<long?>(null, "Failed to add segment"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding segment");
            return StatusCode(500, new ApiResponse<long?>(null, "Internal server error"));
        }
    }

    /// <summary>
    /// Delete a segment between two points
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteSegment(
        [FromQuery] long fromId,
        [FromQuery] long toId)
    {
        try
        {
            var deleted = await _spaceDbService.DeleteSegmentAsync(fromId, toId);
            
            if (deleted)
            {
                return Ok(new ApiResponse<bool>(true, $"Segment from {fromId} to {toId} deleted successfully"));
            }
            else
            {
                return NotFound(new ApiResponse<bool>(false, $"Segment from {fromId} to {toId} not found"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting segment");
            return StatusCode(500, new ApiResponse<bool>(false, "Internal server error"));
        }
    }
}

