using Magic.Kernel.Devices;
using Magic.Kernel.Devices.Streams.Drivers;
using Magic.Kernel.Devices.Streams.Views;
using Magic.Kernel.Interpretation;

namespace Magic.Kernel.Devices.Streams
{
    /// <summary>
    /// Site device: an HTTP server that listens on a configurable port and serves HTML views by route.
    /// Views are resolved from type definitions in the executable unit (types with RenderDevice generalization).
    /// Usage in AGI:
    /// <code>
    /// Login{} : view {
    ///   Username: field&lt;string&gt;(label: "Username");
    ///   Password: field&lt;string&gt;(type: "password");
    ///   Logon: button;
    ///   Error: bool;
    ///
    ///   method Render() {
    ///     return html: &lt;html&gt;...&lt;/html&gt;;
    ///   }
    /// }
    ///
    /// Site1} : site {
    ///   Login{};
    /// }
    ///
    /// procedure Main() {
    ///   var frontend := stream&lt;site, Site1&gt;;
    ///   frontend.open({ port: 6000 });
    ///   await frontend;
    /// }
    /// </code>
    /// </summary>
    public class SiteStreamDevice : DefStream
    {
        private SiteDriver? _driver;

        /// <summary>Type names of views hosted by this site (e.g. "Login", "Dashboard").</summary>
        public List<string> ViewTypeNames { get; } = new();

        public override async Task<object?> CallObjAsync(string methodName, object?[] args)
        {
            var name = methodName?.Trim() ?? "";

            if (string.Equals(name, "open", StringComparison.OrdinalIgnoreCase))
                return await HandleOpenAsync(args).ConfigureAwait(false);

            throw new CallUnknownMethodException(name, this);
        }

        private async Task<DeviceOperationResult> HandleOpenAsync(object?[]? args)
        {
            _driver ??= new SiteDriver();
            _driver.SetServerName(Name);
            if (args != null && args.Length > 0)
                _driver.ParseAndApplyConfig(args[0]);

            // Resolve view definitions from the executable unit and register them with the driver.
            var unit = ExecutionCallContext?.Unit;
            if (unit != null)
                _driver.RegisterViewsFromUnit(unit, ViewTypeNames);

            return await _driver.OpenAsync().ConfigureAwait(false);
        }

        private IStreamDevice Driver => _driver ?? throw new InvalidOperationException("Device not opened. Call site.open first.");

        public override Task<DeviceOperationResult> OpenAsync() => Driver.OpenAsync();

        public override async Task<object?> AwaitObjAsync()
        {
            if (_driver != null)
                await _driver.AwaitUntilStoppedAsync().ConfigureAwait(false);
            return this;
        }

        public override Task<object?> Await() => AwaitObjAsync();

        public override async Task<DeviceOperationResult> CloseAsync()
        {
            try
            {
                if (_driver != null)
                    await _driver.CloseAsync().ConfigureAwait(false);
            }
            finally
            {
                UnregisterFromStreamRegistry();
            }

            return DeviceOperationResult.Success;
        }

        public override Task<(DeviceOperationResult Result, byte[] Bytes)> ReadAsync() => Driver.ReadAsync();
        public override Task<DeviceOperationResult> WriteAsync(byte[] bytes) => Driver.WriteAsync(bytes);
        public override Task<DeviceOperationResult> ControlAsync(DeviceControlBase deviceControl) => Driver.ControlAsync(deviceControl);
        public override Task<(DeviceOperationResult Result, IStreamChunk? Chunk)> ReadChunkAsync() => Driver.ReadChunkAsync();
        public override Task<DeviceOperationResult> WriteChunkAsync(IStreamChunk chunk) => Driver.WriteChunkAsync(chunk);
        public override Task<DeviceOperationResult> MoveAsync(StructurePosition? position) => Driver.MoveAsync(position);
        public override Task<(DeviceOperationResult Result, long Length)> LengthAsync() => Driver.LengthAsync();
    }
}
