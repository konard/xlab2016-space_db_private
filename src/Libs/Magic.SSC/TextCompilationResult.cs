using System;

namespace Magic.SSC
{
    /// <summary>Result of text compilation: vertices in order and relation ordinals (paragraph index → sentence index).</summary>
    public class TextCompilationResult
    {
        public IReadOnlyList<Magic.Kernel.Space.Vertex> Vertices { get; init; } = Array.Empty<Magic.Kernel.Space.Vertex>();
        /// <summary>Pairs (fromOrdinal, toOrdinal) into Vertices list for paragraph → sentence.</summary>
        public IReadOnlyList<(int FromOrdinal, int ToOrdinal)> RelationOrdinals { get; init; } = Array.Empty<(int, int)>();
    }
}
