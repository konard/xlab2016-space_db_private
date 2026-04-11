using Magic.Kernel.Devices.Streams.Drivers;
using Magic.Kernel.Devices.Streams.Views;
using Magic.Kernel.Interpretation;

namespace Magic.Kernel.Devices.Streams
{
    /// <summary>
    /// Render device: wraps a <see cref="ViewDefinition"/> and exposes rendering capability
    /// as a generalization that can be attached to view-type def-objects.
    /// This enables: <c>RenderDriver.RenderToHtml(view.RenderResult)</c> from site routing.
    /// </summary>
    public sealed class RenderDevice : DefStream
    {
        private ViewDefinition? _viewDefinition;

        /// <summary>The view definition this render device represents.</summary>
        public ViewDefinition? ViewDefinition
        {
            get => _viewDefinition;
            set => _viewDefinition = value;
        }

        /// <summary>Renders the view to an HTML string using <see cref="RenderDriver"/>.</summary>
        public string RenderHtml()
        {
            return _viewDefinition?.RenderHtml() ?? "<html><body></body></html>";
        }

        public override Task<object?> CallObjAsync(string methodName, object?[] args)
        {
            var name = methodName?.Trim() ?? "";

            if (string.Equals(name, "render", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult<object?>(RenderHtml());

            throw new CallUnknownMethodException(name, this);
        }

        public override Task<DeviceOperationResult> OpenAsync() =>
            Task.FromResult(DeviceOperationResult.Success);

        public override Task<object?> AwaitObjAsync() =>
            Task.FromResult<object?>(this);

        public override Task<object?> Await() =>
            Task.FromResult<object?>(this);

        public override Task<DeviceOperationResult> CloseAsync()
        {
            UnregisterFromStreamRegistry();
            return Task.FromResult(DeviceOperationResult.Success);
        }

        public override Task<(DeviceOperationResult Result, byte[] Bytes)> ReadAsync() =>
            Task.FromResult((DeviceOperationResult.NotSupported("RenderDevice does not support Read"), Array.Empty<byte>()));

        public override Task<DeviceOperationResult> WriteAsync(byte[] bytes) =>
            Task.FromResult(DeviceOperationResult.NotSupported("RenderDevice does not support Write"));

        public override Task<DeviceOperationResult> ControlAsync(DeviceControlBase deviceControl) =>
            Task.FromResult(DeviceOperationResult.NotSupported("RenderDevice does not support Control"));

        public override Task<(DeviceOperationResult Result, IStreamChunk? Chunk)> ReadChunkAsync() =>
            Task.FromResult<(DeviceOperationResult, IStreamChunk?)>((DeviceOperationResult.NotSupported("RenderDevice does not support ReadChunk"), null));

        public override Task<DeviceOperationResult> WriteChunkAsync(IStreamChunk chunk) =>
            Task.FromResult(DeviceOperationResult.NotSupported("RenderDevice does not support WriteChunk"));

        public override Task<DeviceOperationResult> MoveAsync(StructurePosition? position) =>
            Task.FromResult(DeviceOperationResult.NotSupported("RenderDevice does not support Move"));

        public override Task<(DeviceOperationResult Result, long Length)> LengthAsync() =>
            Task.FromResult((DeviceOperationResult.NotSupported("RenderDevice does not support Length"), 0L));
    }
}
