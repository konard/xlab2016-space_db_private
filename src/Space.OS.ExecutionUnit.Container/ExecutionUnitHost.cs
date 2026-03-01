using Microsoft.Extensions.Options;
using Space.OS.ExecutionUnit;

namespace Space.OS.ExecutionUnit.Container;

/// <summary>Runs ExecutionUnit instances by id using config from ExecutionUnit:Instances.</summary>
public class ExecutionUnitHost : IExecutionUnitHost
{
    private readonly IOptions<ContainerOptions> _options;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly ILoggerFactory _loggerFactory;

    public ExecutionUnitHost(
        IOptions<ContainerOptions> options,
        IConfiguration configuration,
        IWebHostEnvironment env,
        ILoggerFactory loggerFactory)
    {
        _options = options;
        _configuration = configuration;
        _env = env;
        _loggerFactory = loggerFactory;
    }

    public IReadOnlyList<string> GetInstanceIds()
    {
        var instances = _options.Value?.Instances ?? new Dictionary<string, InstanceConfig>();
        return instances.Keys.ToList();
    }

    public async Task<ExecutionResult> RunAsync(string instanceId, string? programCodeOverride = null)
    {
        var instances = _options.Value?.Instances ?? new Dictionary<string, InstanceConfig>();
        if (!instances.TryGetValue(instanceId, out var config) || config == null)
            return new ExecutionResult { Success = false, ErrorMessage = $"Unknown instance: {instanceId}. Known: [{string.Join(", ", instances.Keys)}]." };

        var basePath = _env.ContentRootPath;
        var codeDir = string.IsNullOrWhiteSpace(config.CodeDirectory)
            ? Path.Combine(basePath, "Code")
            : Path.IsPathRooted(config.CodeDirectory)
                ? config.CodeDirectory
                : Path.Combine(basePath, config.CodeDirectory.Trim());

        var programFile = config.ProgramFile ?? "program.agic";
        var vaultFile = config.VaultFile ?? "vault.json";

        // Resolve program path: prefer program.agic then program.agi in codeDir
        string? programPath = null;
        var agicPath = Path.Combine(codeDir, "program.agic");
        var agiPath = Path.Combine(codeDir, "program.agi");
        var configPath = Path.Combine(codeDir, programFile);
        if (File.Exists(agicPath)) programPath = agicPath;
        else if (File.Exists(agiPath)) programPath = agiPath;
        else if (File.Exists(configPath)) programPath = configPath;

        var vaultPath = Path.Combine(codeDir, vaultFile);

        var request = new ExecutionRequest
        {
            ProgramCode = programCodeOverride,
            ProgramPath = programCodeOverride == null ? programPath : null,
            VaultPath = File.Exists(vaultPath) ? vaultPath : null,
            RocksDbPath = config.RocksDbPath,
            SpaceName = config.SpaceName
        };

        var dict = new Dictionary<string, string?>();
        dict["RocksDb:Path"] = config.RocksDbPath ?? _configuration["RocksDb:Path"] ?? "rocksdb";
        dict["SpaceDisk:SpaceName"] = config.SpaceName ?? "";
        var instanceConfig = new ConfigurationBuilder()
            .AddConfiguration(_configuration)
            .AddInMemoryCollection(dict)
            .Build();

        return await ExecutionUnitRunner.RunAsync(request, instanceConfig, _loggerFactory).ConfigureAwait(false);
    }
}
