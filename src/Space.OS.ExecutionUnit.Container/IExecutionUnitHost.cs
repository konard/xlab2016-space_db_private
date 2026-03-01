using Space.OS.ExecutionUnit;

namespace Space.OS.ExecutionUnit.Container;

/// <summary>Host that runs a specific ExecutionUnit instance by id.</summary>
public interface IExecutionUnitHost
{
    /// <summary>Returns registered instance ids.</summary>
    IReadOnlyList<string> GetInstanceIds();

    /// <summary>Runs the instance with given id. Optional programCode overrides file-based program.</summary>
    Task<ExecutionResult> RunAsync(string instanceId, string? programCodeOverride = null);
}
