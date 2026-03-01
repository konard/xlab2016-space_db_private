namespace Space.OS.ExecutionUnit;

public interface IExecutionUnitService
{
    /// <summary>Runs program from Code folder (program.agic or program.agi) and Code/vault.json. If programCode is set, runs that instead of file.</summary>
    Task<ExecutionResult> RunFromCodeFolderAsync(string? programCode = null);
}
