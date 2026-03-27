using System.Collections.Generic;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using Magic.Kernel.Compilation;

namespace Space.OS.Terminal;

/// <summary>Подсветка .agi по токенам <see cref="Scanner"/> (тот же лексер, что и компилятор).</summary>
internal sealed class MagicAgiColorizer : DocumentColorizingTransformer
{
    private static readonly Brush PlainBrush = CreateBrush(0xDD, 0xE8, 0xFF);
    private static readonly Brush KeywordBrush = CreateBrush(0x6C, 0x9E, 0xFF);
    private static readonly Brush IdentifierBrush = CreateBrush(0xA8, 0xC5, 0xFF);
    private static readonly Brush StringBrush = CreateBrush(0x7E, 0xDC, 0x9A);
    private static readonly Brush NumberBrush = CreateBrush(0xF5, 0xA8, 0x7A);
    private static readonly Brush CommentBrush = CreateBrush(0x6B, 0x7A, 0x99);
    private static readonly Brush PunctuationBrush = CreateBrush(0x9A, 0xA8, 0xCC);

    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "@", "AGI", "program", "module", "system", "procedure", "function", "entrypoint", "asm",
        "table", "database", "await", "call", "print", "ret", "global", "lambda", "string", "address",
        "index", "indices", "var", "if", "else", "while", "loop"
    };

    private IReadOnlyList<(int start, int end, Brush brush)> _spans = [];

    private static SolidColorBrush CreateBrush(byte r, byte g, byte bl)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, bl));
        brush.Freeze();
        return brush;
    }

    public void DocumentUpdated(TextDocument document)
    {
        var text = document.Text ?? string.Empty;
        var scanner = new Scanner(text, emitLineComments: true);
        var list = new List<(int, int, Brush)>();
        while (true)
        {
            var t = scanner.Scan();
            if (t.IsEndOfInput)
            {
                break;
            }

            list.Add((t.Start, t.End, BrushFor(t)));
        }

        _spans = list;
    }

    private static Brush BrushFor(Token t)
    {
        return t.Kind switch
        {
            TokenKind.LineComment => CommentBrush,
            TokenKind.StringLiteral => StringBrush,
            TokenKind.Number or TokenKind.Float => NumberBrush,
            TokenKind.Identifier => Keywords.Contains(t.Value) ? KeywordBrush : IdentifierBrush,
            TokenKind.Newline => PlainBrush,
            _ when t.Kind is TokenKind.Colon or TokenKind.Comma or TokenKind.LBracket or TokenKind.RBracket
                or TokenKind.LParen or TokenKind.RParen or TokenKind.LBrace or TokenKind.RBrace
                or TokenKind.Dot or TokenKind.Assign or TokenKind.LessThan or TokenKind.GreaterThan
                or TokenKind.Semicolon => PunctuationBrush,
            _ => PlainBrush
        };
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        // ChangeLinePart допускает только смещения по *телу* строки (без \r\n). См. DocumentColorizingTransformer.
        var lineStart = line.Offset;
        var lineContentEnd = line.Offset + line.Length;
        foreach (var (start, end, brush) in _spans)
        {
            if (end <= lineStart || start >= lineContentEnd)
            {
                continue;
            }

            var s = System.Math.Max(start, lineStart);
            var e = System.Math.Min(end, lineContentEnd);
            if (s < e)
            {
                ChangeLinePart(s, e, element => element.TextRunProperties.SetForegroundBrush(brush));
            }
        }
    }
}
