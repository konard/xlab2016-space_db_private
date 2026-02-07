using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceDb.Services;

namespace SpaceDb.Controllers
{
    /// <summary>
    /// Controller for managing Links storage operations
    /// Provides an alternative graph storage mechanism using Platform.Data.Doublets
    /// </summary>
    [ApiController]
    [Route("api/v1/links")]
    [Authorize]
    public class LinksController : ControllerBase
    {
        private readonly ILinksService _linksService;
        private readonly ILogger<LinksController> _logger;

        public LinksController(ILinksService linksService, ILogger<LinksController> logger)
        {
            _linksService = linksService ?? throw new ArgumentNullException(nameof(linksService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Create a new link with source and target
        /// </summary>
        /// <param name="request">Link creation request</param>
        /// <returns>Created link ID</returns>
        [HttpPost]
        public async Task<IActionResult> CreateLink([FromBody] CreateLinkRequest request)
        {
            try
            {
                var linkId = await _linksService.CreateLinkAsync(request.Source, request.Target);
                _logger.LogInformation("Created link {LinkId}: {Source} -> {Target}",
                    linkId, request.Source, request.Target);

                return Ok(new
                {
                    data = new { linkId, source = request.Source, target = request.Target },
                    message = $"Link {linkId} created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating link: {Source} -> {Target}",
                    request.Source, request.Target);
                return StatusCode(500, new { message = "Failed to create link", error = ex.Message });
            }
        }

        /// <summary>
        /// Get link by ID
        /// </summary>
        /// <param name="id">Link ID</param>
        /// <returns>Link data</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLink(ulong id)
        {
            try
            {
                var link = await _linksService.GetLinkAsync(id);
                if (!link.HasValue)
                {
                    return NotFound(new { message = $"Link {id} not found" });
                }

                return Ok(new
                {
                    data = new { id, source = link.Value.source, target = link.Value.target },
                    message = "Link retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving link {Id}", id);
                return StatusCode(500, new { message = "Failed to retrieve link", error = ex.Message });
            }
        }

        /// <summary>
        /// Update existing link
        /// </summary>
        /// <param name="id">Link ID to update</param>
        /// <param name="request">Update request</param>
        /// <returns>Updated link data</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLink(ulong id, [FromBody] UpdateLinkRequest request)
        {
            try
            {
                var updatedId = await _linksService.UpdateLinkAsync(id, request.NewSource, request.NewTarget);
                _logger.LogInformation("Updated link {LinkId} to {Source} -> {Target}",
                    updatedId, request.NewSource, request.NewTarget);

                return Ok(new
                {
                    data = new { linkId = updatedId, source = request.NewSource, target = request.NewTarget },
                    message = $"Link {updatedId} updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating link {Id}", id);
                return StatusCode(500, new { message = "Failed to update link", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete link by ID
        /// </summary>
        /// <param name="id">Link ID to delete</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLink(ulong id)
        {
            try
            {
                await _linksService.DeleteLinkAsync(id);
                _logger.LogInformation("Deleted link {LinkId}", id);

                return Ok(new { message = $"Link {id} deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting link {Id}", id);
                return StatusCode(500, new { message = "Failed to delete link", error = ex.Message });
            }
        }

        /// <summary>
        /// Search links by source
        /// </summary>
        /// <param name="source">Source link ID</param>
        /// <returns>Collection of matching link IDs</returns>
        [HttpGet("search/by-source/{source}")]
        public async Task<IActionResult> SearchBySource(ulong source)
        {
            try
            {
                var results = await _linksService.SearchBySourceAsync(source);
                var linkIds = results.ToList();

                return Ok(new
                {
                    data = linkIds,
                    count = linkIds.Count,
                    message = $"Found {linkIds.Count} links with source {source}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching by source {Source}", source);
                return StatusCode(500, new { message = "Failed to search by source", error = ex.Message });
            }
        }

        /// <summary>
        /// Search links by target
        /// </summary>
        /// <param name="target">Target link ID</param>
        /// <returns>Collection of matching link IDs</returns>
        [HttpGet("search/by-target/{target}")]
        public async Task<IActionResult> SearchByTarget(ulong target)
        {
            try
            {
                var results = await _linksService.SearchByTargetAsync(target);
                var linkIds = results.ToList();

                return Ok(new
                {
                    data = linkIds,
                    count = linkIds.Count,
                    message = $"Found {linkIds.Count} links with target {target}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching by target {Target}", target);
                return StatusCode(500, new { message = "Failed to search by target", error = ex.Message });
            }
        }

        /// <summary>
        /// Search links by both source and target
        /// </summary>
        /// <param name="source">Source link ID</param>
        /// <param name="target">Target link ID</param>
        /// <returns>Collection of matching link IDs</returns>
        [HttpGet("search")]
        public async Task<IActionResult> SearchBySourceAndTarget([FromQuery] ulong source, [FromQuery] ulong target)
        {
            try
            {
                var results = await _linksService.SearchBySourceAndTargetAsync(source, target);
                var linkIds = results.ToList();

                return Ok(new
                {
                    data = linkIds,
                    count = linkIds.Count,
                    message = $"Found {linkIds.Count} links with source {source} and target {target}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching by source {Source} and target {Target}", source, target);
                return StatusCode(500, new { message = "Failed to search by source and target", error = ex.Message });
            }
        }

        /// <summary>
        /// Get total count of links
        /// </summary>
        /// <returns>Total link count</returns>
        [HttpGet("count")]
        public async Task<IActionResult> CountLinks()
        {
            try
            {
                var count = await _linksService.CountLinksAsync();

                return Ok(new
                {
                    data = count,
                    message = $"Total links: {count}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting links");
                return StatusCode(500, new { message = "Failed to count links", error = ex.Message });
            }
        }

        /// <summary>
        /// Store resource-block-fragment hierarchy using Links
        /// </summary>
        /// <param name="request">Hierarchy storage request</param>
        /// <returns>Root link ID for the resource hierarchy</returns>
        [HttpPost("resource-hierarchy")]
        public async Task<IActionResult> StoreResourceHierarchy([FromBody] StoreResourceHierarchyRequest request)
        {
            try
            {
                var resourceLinkId = await _linksService.StoreResourceHierarchyAsync(
                    request.ResourceId,
                    request.BlockIds,
                    request.FragmentIdsByBlock);

                _logger.LogInformation("Stored resource hierarchy {ResourceLinkId} for resource {ResourceId}",
                    resourceLinkId, request.ResourceId);

                return Ok(new
                {
                    data = new { resourceLinkId, resourceId = request.ResourceId },
                    message = $"Resource hierarchy stored successfully with link ID {resourceLinkId}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing resource hierarchy for resource {ResourceId}", request.ResourceId);
                return StatusCode(500, new { message = "Failed to store resource hierarchy", error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieve resource hierarchy from Links
        /// </summary>
        /// <param name="resourceLinkId">Root resource link ID</param>
        /// <returns>Resource hierarchy data</returns>
        [HttpGet("resource-hierarchy/{resourceLinkId}")]
        public async Task<IActionResult> GetResourceHierarchy(ulong resourceLinkId)
        {
            try
            {
                var hierarchy = await _linksService.GetResourceHierarchyAsync(resourceLinkId);
                if (hierarchy == null)
                {
                    return NotFound(new { message = $"Resource hierarchy not found for link ID {resourceLinkId}" });
                }

                return Ok(new
                {
                    data = hierarchy,
                    message = $"Retrieved resource hierarchy for link ID {resourceLinkId}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving resource hierarchy for link {ResourceLinkId}", resourceLinkId);
                return StatusCode(500, new { message = "Failed to retrieve resource hierarchy", error = ex.Message });
            }
        }
    }

    // Request DTOs
    public class CreateLinkRequest
    {
        public ulong Source { get; set; }
        public ulong Target { get; set; }
    }

    public class UpdateLinkRequest
    {
        public ulong NewSource { get; set; }
        public ulong NewTarget { get; set; }
    }

    public class StoreResourceHierarchyRequest
    {
        public long ResourceId { get; set; }
        public List<long> BlockIds { get; set; } = new();
        public Dictionary<long, IEnumerable<long>> FragmentIdsByBlock { get; set; } = new();
    }
}
