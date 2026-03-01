using Magic.Kernel.Interpretation;

namespace Space.OS.ExecutionUnit;

/// <summary>Parameters for running an AGI program (library mode).</summary>
public class ExecutionRequest
{
    /// <summary>Source code to compile and run. Ignored if <see cref="ProgramPath"/> is set.</summary>
    public string? ProgramCode { get; set; }

    /// <summary>Path to program file (.agi or .agic). Takes precedence over <see cref="ProgramCode"/>.</summary>
    public string? ProgramPath { get; set; }

    /// <summary>Path to vault JSON file (e.g. Code/vault.json).</summary>
    public string? VaultPath { get; set; }

    /// <summary>In-memory vault key-value. Used when <see cref="VaultPath"/> is not set.</summary>
    public IReadOnlyDictionary<string, string?>? VaultData { get; set; }

    /// <summary>Explicit vault reader. Overrides VaultPath and VaultData when set.</summary>
    public IVaultReader? VaultReader { get; set; }

    /// <summary>RocksDB directory. When null, uses config or default.</summary>
    public string? RocksDbPath { get; set; }

    /// <summary>Space name for disk key prefix (module|program). When null, taken from program or config.</summary>
    public string? SpaceName { get; set; }
}
