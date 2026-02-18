using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Magic.Kernel.Space;

namespace Magic.SSC
{
    /// <summary>
    /// Compiles plain text into hierarchical structure: paragraphs and sentences as vertices.
    /// Vertices are ordered: paragraphs first, then sentences; RelationOrdinals link paragraph → sentence by list index.
    /// </summary>
    public class TextCompiler
    {
        private static readonly Regex SentenceSplitRegex = new Regex(
            @"(?<=[.!?])\s+(?=[^\s])|(?<=[.!?])$",
            RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>Compiles text into vertices (paragraphs, sentences) and relation ordinals.</summary>
        public TextCompilationResult Compile(string content)
        {
            if (string.IsNullOrEmpty(content))
                return new TextCompilationResult();

            var vertices = new List<Vertex>();
            var relationOrdinals = new List<(int FromOrdinal, int ToOrdinal)>();

            var paragraphs = SplitParagraphs(content);

            for (int p = 0; p < paragraphs.Count; p++)
            {
                var paragraphText = paragraphs[p].Trim();
                if (string.IsNullOrEmpty(paragraphText))
                    continue;

                var paragraphVertex = CreateTextVertex(paragraphText, p, 0);
                int paragraphOrdinal = vertices.Count;
                vertices.Add(paragraphVertex);

                var sentences = SplitSentences(paragraphText);
                for (int s = 0; s < sentences.Count; s++)
                {
                    var sentenceText = sentences[s].Trim();
                    if (string.IsNullOrEmpty(sentenceText))
                        continue;

                    var sentenceVertex = CreateTextVertex(sentenceText, p, s + 1);
                    int sentenceOrdinal = vertices.Count;
                    vertices.Add(sentenceVertex);
                    relationOrdinals.Add((paragraphOrdinal, sentenceOrdinal));
                }
            }

            return new TextCompilationResult
            {
                Vertices = vertices,
                RelationOrdinals = relationOrdinals
            };
        }

        /// <summary>Split text into paragraphs (double newline).</summary>
        public static List<string> SplitParagraphs(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();
            var normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");
            return normalized.Split(new[] { "\n\n" }, StringSplitOptions.None).ToList();
        }

        /// <summary>Split paragraph into sentences (boundaries: . ! ?).</summary>
        public static List<string> SplitSentences(string paragraph)
        {
            if (string.IsNullOrEmpty(paragraph))
                return new List<string>();
            var parts = SentenceSplitRegex.Split(paragraph);
            return parts.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        }

        private static Vertex CreateTextVertex(string text, int paragraphIndex, int sentenceIndex)
        {
            var hash = ComputeHash(text);
            return new Vertex
            {
                Name = hash,
                Position = new Position { Dimensions = new List<float> { paragraphIndex, sentenceIndex } },
                Data = new EntityData
                {
                    Type = new HierarchicalDataType { Types = new List<DataType> { DataType.Text } },
                    Data = ToBase64(text)
                }
            };
        }

        private static string ComputeHash(string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static string ToBase64(string text)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        }
    }
}
