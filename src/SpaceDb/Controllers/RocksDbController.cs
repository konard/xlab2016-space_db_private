using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceDb.Services;
using System.Text.Json;

namespace SpaceDb.Controllers;

/// <summary>
/// RocksDB Key-Value Storage Controller
/// </summary>
[Route("/api/v1/rocksdb")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class RocksDbController : ControllerBase
{
    private readonly IRocksDbService _rocksDbService;
    private readonly ILogger<RocksDbController> _logger;

    public RocksDbController(IRocksDbService rocksDbService, ILogger<RocksDbController> logger)
    {
        _rocksDbService = rocksDbService ?? throw new ArgumentNullException(nameof(rocksDbService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Store a key-value pair
    /// </summary>
    /// <param name="request">Key-value data</param>
    /// <response code="200">Value stored successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("store")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Store([FromBody] KeyValueRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Key))
            {
                return BadRequest(new ApiResponse<bool>(false, "Key cannot be empty"));
            }

            var result = await _rocksDbService.PutJsonStringAsync(request.Key, request.Value ?? string.Empty);
            return Ok(new ApiResponse<bool>(result, result ? "Value stored successfully" : "Failed to store value"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing key-value pair");
            return StatusCode(500, new ApiResponse<bool>(false, "Internal server error"));
        }
    }

    /// <summary>
    /// Retrieve a value by key
    /// </summary>
    /// <param name="key">The key to retrieve</param>
    /// <response code="200">Value retrieved successfully</response>
    /// <response code="404">Key not found</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("get/{key}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(string key)
    {
        try
        {
            var jsonString = await _rocksDbService.GetJsonStringAsync(key);
            if (jsonString == null)
            {
                return NotFound(new ApiResponse<object>(null, "Key not found"));
            }

            var jsonObject = JsonSerializer.Deserialize<object>(jsonString);
            return Ok(new ApiResponse<object>(jsonObject, "Value retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving value for key: {Key}", key);
            return StatusCode(500, new ApiResponse<object>(null, "Internal server error"));
        }
    }

    /// <summary>
    /// Delete a key-value pair
    /// </summary>
    /// <param name="key">The key to delete</param>
    /// <response code="200">Key deleted successfully</response>
    /// <response code="401">Unauthorized</response>
    [HttpDelete("delete/{key}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(string key)
    {
        try
        {
            var result = await _rocksDbService.DeleteAsync(key);
            return Ok(new ApiResponse<bool>(result, result ? "Key deleted successfully" : "Failed to delete key"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting key: {Key}", key);
            return StatusCode(500, new ApiResponse<bool>(false, "Internal server error"));
        }
    }

    /// <summary>
    /// Check if a key exists
    /// </summary>
    /// <param name="key">The key to check</param>
    /// <response code="200">Key existence status</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("exists/{key}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Exists(string key)
    {
        try
        {
            var exists = await _rocksDbService.ExistsAsync(key);
            return Ok(new ApiResponse<bool>(exists, exists ? "Key exists" : "Key does not exist"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking key existence: {Key}", key);
            return StatusCode(500, new ApiResponse<bool>(false, "Internal server error"));
        }
    }

    /// <summary>
    /// Get all key-value pairs
    /// </summary>
    /// <response code="200">All key-value pairs</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("all")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<KeyValueDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            // Используем JSON метод для получения всех записей
            var allPairs = await _rocksDbService.GetAllJsonAsync<object>();
            var result = allPairs.Select(kv => new KeyValueDto
            {
                Key = kv.Key,
                Value = JsonSerializer.Serialize(kv.Value)
            });

            return Ok(new ApiResponse<IEnumerable<KeyValueDto>>(result, "All key-value pairs retrieved"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all key-value pairs");
            return StatusCode(500, new ApiResponse<IEnumerable<KeyValueDto>>(null, "Internal server error"));
        }
    }

    /// <summary>
    /// Get key-value pairs in a range
    /// </summary>
    /// <param name="startKey">Start key</param>
    /// <param name="endKey">End key</param>
    /// <response code="200">Range of key-value pairs</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("range")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<KeyValueDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetRange([FromQuery] string startKey, [FromQuery] string endKey)
    {
        try
        {
            // Используем JSON метод для получения диапазона
            var rangePairs = await _rocksDbService.GetRangeJsonAsync<object>(startKey, endKey);
            var result = rangePairs.Select(kv => new KeyValueDto
            {
                Key = kv.Key,
                Value = JsonSerializer.Serialize(kv.Value)
            });

            return Ok(new ApiResponse<IEnumerable<KeyValueDto>>(result, "Range retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving range from {StartKey} to {EndKey}", startKey, endKey);
            return StatusCode(500, new ApiResponse<IEnumerable<KeyValueDto>>(null, "Internal server error"));
        }
    }

    /// <summary>
    /// Get total count of records
    /// </summary>
    /// <response code="200">Total count</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("count")]
    [ProducesResponseType(typeof(ApiResponse<long>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCount()
    {
        try
        {
            var count = await _rocksDbService.GetCountAsync();
            return Ok(new ApiResponse<long>(count, "Count retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving count");
            return StatusCode(500, new ApiResponse<long>(0, "Internal server error"));
        }
    }

    /// <summary>
    /// Clear all data
    /// </summary>
    /// <response code="200">Data cleared successfully</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("clear")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Clear()
    {
        try
        {
            await _rocksDbService.ClearAsync();
            return Ok(new ApiResponse<bool>(true, "Data cleared successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing data");
            return StatusCode(500, new ApiResponse<bool>(false, "Internal server error"));
        }
    }

    /// <summary>
    /// Compact the database
    /// </summary>
    /// <response code="200">Database compacted successfully</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("compact")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Compact()
    {
        try
        {
            var result = await _rocksDbService.CompactAsync();
            return Ok(new ApiResponse<bool>(result, result ? "Database compacted successfully" : "Failed to compact database"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compacting database");
            return StatusCode(500, new ApiResponse<bool>(false, "Internal server error"));
        }
    }

}

public class KeyValueRequest
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
}

public class KeyValueDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class ApiResponse<T>
{
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool Success => Data != null || Message.Contains("successfully");

    public ApiResponse(T? data, string message)
    {
        Data = data;
        Message = message;
    }
}
