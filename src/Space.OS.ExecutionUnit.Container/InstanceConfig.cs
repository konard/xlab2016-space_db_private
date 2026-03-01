namespace Space.OS.ExecutionUnit.Container;

/// <summary>Configuration for one ExecutionUnit instance (e.g. one Telegram channel).</summary>
public class InstanceConfig
{
    /// <summary>Directory containing program.agi/agic and vault.json. Relative to content root or current directory.</summary>
    public string? CodeDirectory { get; set; }

    /// <summary>Program file name (e.g. program.agic or program.agi). Default: program.agic.</summary>
    public string? ProgramFile { get; set; }

    /// <summary>Vault file name (e.g. vault.json). Default: vault.json.</summary>
    public string? VaultFile { get; set; }

    /// <summary>RocksDB path for this instance. When null, shared or default is used.</summary>
    public string? RocksDbPath { get; set; }

    /// <summary>Space name (disk key prefix). E.g. "telegram|channel_1".</summary>
    public string? SpaceName { get; set; }
}
