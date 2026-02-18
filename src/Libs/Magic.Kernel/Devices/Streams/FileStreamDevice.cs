using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Magic.Kernel.Core;
using Magic.Kernel.Devices;
using Magic.Kernel.Devices.Streams.Drivers;

namespace Magic.Kernel.Devices.Streams
{
    /// <summary>Stream device over a path: uses FileDriver for files, PathDriver for directories.</summary>
    public class FileStreamDevice : IStreamDevice, IType
    {
        private readonly IStreamDevice driver;

        public long? Index { get; set; }
        public string Name { get; set; } = "";
        public List<IType> Generalizations { get; set; } = new List<IType>();

        public FileStreamDevice(string path, int chunkSize = 65536)
        {
            var p = path ?? "";
            if (Directory.Exists(p))
                driver = new PathDriver(p, chunkSize);
            else if (p.Length > 0 && (p[p.Length - 1] == Path.DirectorySeparatorChar || p[p.Length - 1] == Path.AltDirectorySeparatorChar))
                driver = new PathDriver(p.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), chunkSize);
            else
                driver = new FileDriver(p, chunkSize, FileStreamAccess.ReadWrite);
        }

        public Task<DeviceOperationResult> OpenAsync() => driver.OpenAsync();

        public Task<(DeviceOperationResult Result, byte[] Bytes)> ReadAsync() => driver.ReadAsync();

        public Task<DeviceOperationResult> WriteAsync(byte[] bytes) => driver.WriteAsync(bytes);

        public Task<DeviceOperationResult> ControlAsync(DeviceControlBase deviceControl) => driver.ControlAsync(deviceControl);

        public Task<DeviceOperationResult> CloseAsync() => driver.CloseAsync();

        public Task<(DeviceOperationResult Result, IStreamChunk? Chunk)> ReadChunkAsync() => driver.ReadChunkAsync();

        public Task<DeviceOperationResult> WriteChunkAsync(IStreamChunk chunk) => driver.WriteChunkAsync(chunk);

        public Task<DeviceOperationResult> MoveAsync(StructurePosition? position) => driver.MoveAsync(position);

        public Task<(DeviceOperationResult Result, long Length)> LengthAsync() => driver.LengthAsync();
    }
}
