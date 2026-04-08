using System;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;

namespace Space.OS.Terminal2;

/// <summary>Подсветка текущей строки при остановке отладчика (1-based номер строки документа).</summary>
internal sealed class AgiDebugCurrentLineRenderer : IBackgroundRenderer
{
    private readonly Func<int> _getDocumentLine;

    private static readonly IBrush Fill = new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xCC, 0x33));

    public AgiDebugCurrentLineRenderer(Func<int> getDocumentLine)
    {
        _getDocumentLine = getDocumentLine;
    }

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        var line = _getDocumentLine();
        if (line <= 0 || textView.Document == null)
            return;

        if (line > textView.Document.LineCount)
            return;

        foreach (var vl in textView.VisualLines)
        {
            if (line < vl.FirstDocumentLine.LineNumber || line > vl.LastDocumentLine.LineNumber)
                continue;

            if (vl.TextLines.Count == 0)
                continue;

            var yTop = vl.GetTextLineVisualYPosition(vl.TextLines[0], VisualYPosition.LineTop);
            var yBot = vl.GetTextLineVisualYPosition(vl.TextLines[vl.TextLines.Count - 1], VisualYPosition.LineBottom);
            var top = yTop - textView.VerticalOffset;
            var height = Math.Max(yBot - yTop, 8);
            drawingContext.DrawRectangle(Fill, null, new Rect(0, top, Math.Max(textView.Bounds.Width, 1), height));
            return;
        }
    }
}
