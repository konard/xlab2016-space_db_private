using Microsoft.AspNetCore.Mvc;
using Space.OS.ExecutionUnit;

namespace Space.OS.ExecutionUnit.Container.Controllers;

[ApiController]
[Route("api/v1")]
public class ContainerController : ControllerBase
{
    private readonly IExecutionUnitHost _host;

    public ContainerController(IExecutionUnitHost host)
    {
        _host = host;
    }

    /// <summary>List registered ExecutionUnit instance ids.</summary>
    [HttpGet("instances")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public IActionResult ListInstances() => Ok(_host.GetInstanceIds());

    /// <summary>Run a specific instance. Body may contain programCode to override file-based program.</summary>
    [HttpPost("instances/{instanceId}/run")]
    [ProducesResponseType(typeof(ExecutionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExecutionResult>> Run(
        string instanceId,
        [FromBody] RunRequest? body = null)
    {
        var result = await _host.RunAsync(instanceId, body?.ProgramCode).ConfigureAwait(false);
        if (!result.Success && result.ErrorMessage != null)
        {
            if (result.ErrorMessage.Contains("Unknown instance"))
                return NotFound(result);
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>Run default instance (id = "default" if present).</summary>
    [HttpPost("run")]
    [ProducesResponseType(typeof(ExecutionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExecutionResult>> RunDefault([FromBody] RunRequest? body = null)
    {
        var ids = _host.GetInstanceIds();
        var instanceId = ids.Contains("default") ? "default" : (ids.Count > 0 ? ids[0] : null);
        if (instanceId == null)
            return BadRequest(new ExecutionResult { Success = false, ErrorMessage = "No instances configured. Add ExecutionUnit:Instances in appsettings.json." });
        var result = await _host.RunAsync(instanceId, body?.ProgramCode).ConfigureAwait(false);
        if (!result.Success && result.ErrorMessage != null)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health() => Ok(new { status = "healthy" });

    public class RunRequest
    {
        public string? ProgramCode { get; set; }
    }
}
