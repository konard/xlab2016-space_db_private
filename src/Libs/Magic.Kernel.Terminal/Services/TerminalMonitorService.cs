using Magic.Kernel.Terminal.Models;
using Magic.Kernel;
using Magic.Kernel.Devices.Streams;

namespace Magic.Kernel.Terminal.Services;

public sealed class TerminalMonitorService
{
    public MonitorSnapshot CreateSnapshot(MagicKernel? kernel, IEnumerable<RuntimeExecutionRecord>? records = null)
    {
        var snapshot = new MonitorSnapshot();
        var activeStreams = DefStream.GetActiveStreams();
        var activeClawByUnit = activeStreams
            .OfType<ClawStreamDevice>()
            .Where(x => x.IsListening && !string.IsNullOrWhiteSpace(x.UnitName))
            .Select(x => x.UnitName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (records != null)
        {
            foreach (var exe in records.OrderByDescending(x => x.StartedAtUtc))
            {
                var normalizedState = xStatus(exe.Status, exe.Name, activeClawByUnit);
                var ended = exe.EndedAtUtc.HasValue ? $"; ended={exe.EndedAtUtc:O}" : string.Empty;
                var error = string.IsNullOrWhiteSpace(exe.ErrorMessage) ? string.Empty : $"; error={exe.ErrorMessage}";
                snapshot.Exes.Add(new MonitorRow
                {
                    Name = exe.Name,
                    Type = "program",
                    State = normalizedState,
                    Details = $"instances={exe.InstanceCount}; source={exe.SourcePath}; started={exe.StartedAtUtc:O}{ended}{error}"
                });
            }
        }

        if (kernel != null)
        {
            snapshot.TotalDevices = kernel.Devices.Count;
            foreach (var device in kernel.Devices)
            {
                var row = new MonitorRow
                {
                    Name = device.Name,
                    Type = device.GetType().Name,
                    State = "attached",
                    Details = device.GetType().FullName ?? device.GetType().Name
                };

                if (DeviceClassification.IsStream(device))
                {
                    snapshot.Streams.Add(row);
                }

                if (DeviceClassification.IsDriver(device))
                {
                    snapshot.Drivers.Add(row);
                }
            }
        }

        foreach (var stream in activeStreams)
        {
            var streamName = string.IsNullOrWhiteSpace(stream.Name) ? "(unnamed)" : stream.Name;
            var state = "active";
            var details = stream.GetType().FullName ?? stream.GetType().Name;
            if (stream is ClawStreamDevice claw)
            {
                state = claw.IsListening ? "listening" : "idle";
                details = $"{details}; port={claw.Port}; unit={claw.UnitName}; instance={claw.UnitInstanceIndex?.ToString() ?? "n/a"}";
            }

            if (snapshot.Streams.Any(x => string.Equals(x.Name, streamName, StringComparison.Ordinal) &&
                                          string.Equals(x.Type, stream.GetType().Name, StringComparison.Ordinal)))
            {
                continue;
            }

            snapshot.Streams.Add(new MonitorRow
            {
                Name = streamName,
                Type = stream.GetType().Name,
                State = state,
                Details = details
            });
        }

        snapshot.Connections.Add(new MonitorRow
        {
            Name = "runtime",
            Type = "logical",
            State = "n/a",
            Details = "Connection telemetry is not exposed by current kernel API."
        });

        return snapshot;
    }

    private static string xStatus(string original, string unitName, ISet<string> activeClawByUnit)
    {
        if (string.Equals(original, "stopped", StringComparison.OrdinalIgnoreCase) &&
            activeClawByUnit.Contains(unitName))
        {
            return "running";
        }

        return original;
    }
}
