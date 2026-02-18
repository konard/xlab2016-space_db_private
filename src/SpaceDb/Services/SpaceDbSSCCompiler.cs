using System.Text;
using Magic.Kernel.Devices;
using Magic.Kernel.Devices.SSC;
using Magic.Kernel.Space;
using Magic.SSC;

namespace SpaceDb.Services
{
    /// <summary>
    /// SSC compiler for SpaceDb: delegates text compilation to TextCompiler (Magic.SSC), persists to disk.
    /// Supports only DataFormat.Text; other formats throw NotSupportedException.
    /// </summary>
    public class SpaceDbSSCCompiler : ISSCompiler
    {
        private readonly ISpaceDisk _disk;
        private readonly TextCompiler _textCompiler;

        public SpaceDbSSCCompiler(ISpaceDisk disk, TextCompiler textCompiler)
        {
            _disk = disk ?? throw new ArgumentNullException(nameof(disk));
            _textCompiler = textCompiler ?? throw new ArgumentNullException(nameof(textCompiler));
        }

        public async Task<SSCResult> CompileAsync(IStreamDevice device, ISpaceDisk disk)
        {
            if (device == null || disk == null)
                return new SSCResult { IsSuccess = false, ErrorMessage = "Device or disk is null." };

            var (result, chunk) = await device.ReadChunkAsync().ConfigureAwait(false);
            if (!result.IsSuccess || chunk?.Data == null || chunk.Data.Length == 0)
                return new SSCResult { IsSuccess = false, ErrorMessage = result.ErrorMessage ?? "No chunk data." };

            if (chunk.DataFormat != DataFormat.Text)
                throw new NotSupportedException($"Only DataFormat.Text is supported. Got: {chunk.DataFormat}.");

            var content = Encoding.UTF8.GetString(chunk.Data);
            var compiled = _textCompiler.Compile(content);
            var sscResult = new SSCResult { IsSuccess = true };
            string? spaceName = null;

            try
            {
                var indices = new List<long>();
                foreach (var vertex in compiled.Vertices)
                {
                    var addResult = await disk.AddVertex(vertex, spaceName).ConfigureAwait(false);
                    if (addResult != SpaceOperationResult.Success)
                    {
                        sscResult.IsSuccess = false;
                        sscResult.ErrorMessage = $"AddVertex: {addResult}";
                        return sscResult;
                    }
                    indices.Add(vertex.Index!.Value);
                    sscResult.VertexIndices.Add(vertex.Index!.Value);
                }

                foreach (var (fromOrd, toOrd) in compiled.RelationOrdinals)
                {
                    var relation = new Relation
                    {
                        FromIndex = indices[fromOrd],
                        ToIndex = indices[toOrd],
                        FromType = EntityType.Vertex,
                        ToType = EntityType.Vertex
                    };
                    var addRelResult = await disk.AddRelation(relation, spaceName).ConfigureAwait(false);
                    if (addRelResult != SpaceOperationResult.Success)
                    {
                        sscResult.IsSuccess = false;
                        sscResult.ErrorMessage = $"AddRelation({relation.FromIndex}->{relation.ToIndex}): {addRelResult}";
                        return sscResult;
                    }
                    sscResult.RelationIndices.Add(relation.Index!.Value);
                }
            }
            catch (Exception ex)
            {
                sscResult.IsSuccess = false;
                sscResult.ErrorMessage = ex.Message;
            }

            return sscResult;
        }
    }
}
