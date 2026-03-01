using Microsoft.AspNetCore.Mvc;

namespace Space.OS.ExecutionUnit.Controllers;

[ApiController]
[Route("api/v1")]
public class ExecutionController : ControllerBase
{
    private readonly IExecutionUnitService _service;
    private readonly IConfiguration _configuration;

    public ExecutionController(IExecutionUnitService service, IConfiguration configuration)
    {
        _service = service;
        _configuration = configuration;
    }

    /// <summary>Runs the program from Code/program.agic or Code/program.agi with Code/vault.json.</summary>
    [HttpPost("run")]
    [ProducesResponseType(typeof(ExecutionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExecutionResult>> Run([FromBody] RunOverride? body = null)
    {
        var result = await _service.RunFromCodeFolderAsync(body?.ProgramCode).ConfigureAwait(false);
        if (!result.Success && result.ErrorMessage != null)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health() => Ok(new { status = "healthy" });

    public class RunOverride
    {
        /// <summary>If set, run this code instead of Code/program.*.</summary>
        public string? ProgramCode { get; set; }
    }
}
