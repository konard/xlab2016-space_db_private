using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magic.Kernel.Core;
using Magic.Kernel.Devices;

namespace Magic.Kernel.Devices.Streams
{
    /// <summary>Generic stream device wrapper; override or use for in-memory / test streams.</summary>
    public class StreamDevice : IStreamDevice, IType
    {
        private readonly Func<byte[]?>? readAll;
        private readonly Action<byte[]?>? writeAll;
        private long length;
        private long position;

        public long? Index { get; set; }
        public string Name { get; set; } = "";
        public List<IType> Generalizations { get; set; } = new List<IType>();

        public StreamDevice(Func<byte[]?>? readAll = null, Action<byte[]?>? writeAll = null, long length = 0)
        {
            this.readAll = readAll;
            this.writeAll = writeAll;
            this.length = length;
        }

        public Task<DeviceOperationResult> OpenAsync() => Task.FromResult(DeviceOperationResult.Success);

        public Task<(DeviceOperationResult Result, byte[] Bytes)> ReadAsync()
        {
            var bytes = readAll?.Invoke() ?? Array.Empty<byte>();
            return Task.FromResult((DeviceOperationResult.Success, bytes));
        }

        public Task<DeviceOperationResult> WriteAsync(byte[] bytes)
        {
            writeAll?.Invoke(bytes);
            return Task.FromResult(DeviceOperationResult.Success);
        }

        public Task<DeviceOperationResult> ControlAsync(DeviceControlBase deviceControl) => Task.FromResult(DeviceOperationResult.Success);

        public Task<DeviceOperationResult> CloseAsync() => Task.FromResult(DeviceOperationResult.Success);

        public Task<(DeviceOperationResult Result, IStreamChunk? Chunk)> ReadChunkAsync()
        {
            var data = readAll?.Invoke() ?? Array.Empty<byte>();
            var chunk = new StreamChunk { ChunkSize = data?.Length ?? 0, Data = data ?? Array.Empty<byte>() };
            return Task.FromResult((DeviceOperationResult.Success, (IStreamChunk?)chunk));
        }

        public Task<DeviceOperationResult> WriteChunkAsync(IStreamChunk chunk)
        {
            writeAll?.Invoke(chunk?.Data);
            return Task.FromResult(DeviceOperationResult.Success);
        }

        public Task<DeviceOperationResult> MoveAsync(StructurePosition? position)
        {
            long pos = position == null ? 0 : (position.AbsolutePosition != 0 ? position.AbsolutePosition : position.RelativeIndex);
            this.position = Math.Max(0, pos);
            return Task.FromResult(DeviceOperationResult.Success);
        }

        public Task<(DeviceOperationResult Result, long Length)> LengthAsync()
            => Task.FromResult((DeviceOperationResult.Success, length));
    }
}
