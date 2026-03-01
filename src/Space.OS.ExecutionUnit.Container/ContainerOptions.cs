namespace Space.OS.ExecutionUnit.Container;

/// <summary>Container configuration: named ExecutionUnit instances.</summary>
public class ContainerOptions
{
    public const string SectionName = "ExecutionUnit";

    /// <summary>Named instance configs. Key = instance id (e.g. "default", "telegram_channel_1").</summary>
    public Dictionary<string, InstanceConfig> Instances { get; set; } = new();
}
