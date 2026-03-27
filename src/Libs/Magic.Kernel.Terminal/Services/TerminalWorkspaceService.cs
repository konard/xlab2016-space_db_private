using System.Text.Json;
using System.Text.Json.Nodes;
using Magic.Kernel.Core;
using Magic.Kernel.Terminal.Models;

namespace Magic.Kernel.Terminal.Services;

public sealed class TerminalWorkspaceService
{
    private static readonly string[] SensitiveMarkers = ["password", "secret", "token", "key", "credential", "jwt", "auth"];

    public List<ExecutionUnitItem> LoadExecutionUnits(string spaceConfigPath)
    {
        var root = LoadJsonObject(spaceConfigPath);
        var list = new List<ExecutionUnitItem>();
        var units = root["executionUnits"] as JsonArray;
        if (units is null)
        {
            return list;
        }

        foreach (var item in units)
        {
            if (item is not JsonObject node)
            {
                continue;
            }

            var path = node["path"]?.GetValue<string>() ?? string.Empty;
            var instanceCount = node["instanceCount"]?.GetValue<int>() ?? 1;
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            list.Add(new ExecutionUnitItem
            {
                Path = path,
                InstanceCount = instanceCount > 0 ? instanceCount : 1
            });
        }

        return list.OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public void SaveExecutionUnits(string spaceConfigPath, IEnumerable<ExecutionUnitItem> units)
    {
        var root = LoadJsonObject(spaceConfigPath);
        var array = new JsonArray();
        foreach (var unit in units.Where(x => !string.IsNullOrWhiteSpace(x.Path)))
        {
            array.Add(new JsonObject
            {
                ["path"] = unit.Path.Trim(),
                ["instanceCount"] = unit.InstanceCount > 0 ? unit.InstanceCount : 1
            });
        }

        root["executionUnits"] = array;
        SaveJsonObject(spaceConfigPath, root);
    }

    public List<VaultItem> LoadVaultItems(string? vaultPath = null)
    {
        var resolvedPath = ResolveVaultPath(vaultPath);
        var root = LoadJsonArray(resolvedPath);
        var result = new List<VaultItem>();
        foreach (var node in root)
        {
            if (node is not JsonObject item)
            {
                continue;
            }

            var store = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (item["store"] is JsonObject storeNode)
            {
                foreach (var p in storeNode)
                {
                    store[p.Key] = JsonNodeToObject(p.Value);
                }
            }

            result.Add(new VaultItem
            {
                Program = item["program"]?.GetValue<string>() ?? string.Empty,
                System = item["system"]?.GetValue<string>() ?? string.Empty,
                Module = item["module"]?.GetValue<string>() ?? string.Empty,
                InstanceIndex = item["instanceIndex"]?.GetValue<int>() ?? 0,
                Store = store
            });
        }

        return result;
    }

    public void SaveVaultItems(IEnumerable<VaultItem> items, string? vaultPath = null)
    {
        var resolvedPath = ResolveVaultPath(vaultPath);
        var root = new JsonArray();
        foreach (var item in items)
        {
            var storeObj = new JsonObject();
            foreach (var kv in item.Store)
            {
                storeObj[kv.Key] = JsonValue.Create(kv.Value?.ToString());
            }

            root.Add(new JsonObject
            {
                ["program"] = item.Program,
                ["system"] = item.System,
                ["module"] = item.Module,
                ["instanceIndex"] = item.InstanceIndex,
                ["store"] = storeObj
            });
        }

        var dir = Path.GetDirectoryName(resolvedPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(resolvedPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    public List<VaultStoreRow> ToRows(VaultItem item, bool includeSensitiveValues)
    {
        return item.Store
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x =>
            {
                var sensitive = IsSensitiveKey(x.Key);
                var value = x.Value?.ToString() ?? string.Empty;
                if (sensitive && !includeSensitiveValues)
                {
                    value = Mask(value);
                }

                return new VaultStoreRow
                {
                    Key = x.Key,
                    Value = value,
                    IsSensitive = sensitive
                };
            })
            .ToList();
    }

    public void ApplyRows(VaultItem item, IEnumerable<VaultStoreRow> rows)
    {
        item.Store.Clear();
        foreach (var row in rows.Where(x => !string.IsNullOrWhiteSpace(x.Key)))
        {
            item.Store[row.Key.Trim()] = row.Value;
        }
    }

    private static string ResolveVaultPath(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            return Path.GetFullPath(path);
        }

        return SpaceEnvironment.GetFilePath("vault.json");
    }

    private static JsonObject LoadJsonObject(string path)
    {
        if (!File.Exists(path))
        {
            return new JsonObject();
        }

        var text = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(text))
        {
            return new JsonObject();
        }

        return JsonNode.Parse(text) as JsonObject ?? new JsonObject();
    }

    private static JsonArray LoadJsonArray(string path)
    {
        if (!File.Exists(path))
        {
            return new JsonArray();
        }

        var text = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(text))
        {
            return new JsonArray();
        }

        return JsonNode.Parse(text) as JsonArray ?? new JsonArray();
    }

    private static void SaveJsonObject(string path, JsonObject root)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static bool IsSensitiveKey(string key)
        => SensitiveMarkers.Any(marker => key.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static string Mask(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Length <= 4)
        {
            return "****";
        }

        return $"{value[..2]}***{value[^2..]}";
    }

    private static object? JsonNodeToObject(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        if (node is JsonValue value)
        {
            return value.ToJsonString().Trim('"');
        }

        return node.ToJsonString();
    }
}
