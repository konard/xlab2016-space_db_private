using Magic.Kernel.Devices;

namespace Magic.Kernel.Terminal.Models;

public sealed class ExecutionUnitItem
{
    public string Path { get; set; } = string.Empty;
    public int InstanceCount { get; set; } = 1;

    public string Id { get; set; } = string.Empty;
    public string? CodeDirectory { get; set; }
    public string? ProgramFile { get; set; }
    public string? VaultFile { get; set; }
    public string? RocksDbPath { get; set; }
    public string? SpaceName { get; set; }
}

public sealed class VaultItem
{
    public string Program { get; set; } = string.Empty;
    public string System { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public int InstanceIndex { get; set; }
    public Dictionary<string, object?> Store { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class VaultStoreRow
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsSensitive { get; set; }
}

public sealed class MonitorRow
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string State { get; set; } = "n/a";
    public string Details { get; set; } = string.Empty;
}

public sealed class MonitorSnapshot
{
    public List<MonitorRow> Exes { get; } = new();
    public List<MonitorRow> Streams { get; } = new();
    public List<MonitorRow> Drivers { get; } = new();
    public List<MonitorRow> Connections { get; } = new();
    public int TotalDevices { get; set; }
}

public sealed class RuntimeExecutionRecord
{
    public string Status { get; set; } = "starting";
    public string Name { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? EndedAtUtc { get; set; }
    public bool Success { get; set; }
    public int InstanceCount { get; set; } = 1;
    public string? ErrorMessage { get; set; }
}

public static class DeviceClassification
{
    public static bool IsDriver(IDevice device)
        => device.GetType().Namespace?.Contains(".Drivers", StringComparison.OrdinalIgnoreCase) == true ||
           device.GetType().Name.EndsWith("Driver", StringComparison.OrdinalIgnoreCase);

    public static bool IsStream(IDevice device)
        => device.GetType().Name.Contains("Stream", StringComparison.OrdinalIgnoreCase);
}
