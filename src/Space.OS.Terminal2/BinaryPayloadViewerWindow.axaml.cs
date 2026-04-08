using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Magic.Kernel.Interpretation;

namespace Space.OS.Terminal2;

/// <summary>Hex-просмотр больших <see cref="BinaryDebugPayload"/> вне основного дерева (без зависания UI).</summary>
public sealed class BinaryPayloadViewerWindow : Window
{
    private const int MaxInitialHexBytes = 48 * 1024;
    private readonly ReadOnlyMemory<byte> _data;
    private readonly TextBlock _status;
    private readonly TextBox _body;

    public BinaryPayloadViewerWindow(BinaryDebugPayload payload)
    {
        _data = payload.Data;
        Title = "Двоичные данные · отладчик";
        Width = 780;
        Height = 560;
        MinWidth = 480;
        MinHeight = 320;
        Background = new SolidColorBrush(Color.FromRgb(8, 12, 28));

        var root = new DockPanel { Margin = new Thickness(10) };

        _status = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(0xB0, 0xC4, 0xDE)), // LightSteelBlue
            Margin = new Thickness(0, 0, 0, 8)
        };
        DockPanel.SetDock(_status, Dock.Top);
        root.Children.Add(_status);

        var copyBtn = new Button { Content = "Копировать видимое", Padding = new Thickness(12, 4, 12, 4), Margin = new Thickness(0, 0, 8, 0) };
        copyBtn.Click += async (_, _) => await CopyVisibleAsync();
        var saveBtn = new Button { Content = "Сохранить в файл…", Padding = new Thickness(12, 4, 12, 4) };
        saveBtn.Click += async (_, _) => await SaveToFileAsync();

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 8, 0, 0),
            Children = { copyBtn, saveBtn }
        };
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);

        _body = new TextBox
        {
            IsReadOnly = true,
            FontFamily = new FontFamily("Consolas,Courier New,monospace"),
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0xDC, 0xDC)), // Gainsboro
            Background = new SolidColorBrush(Color.FromRgb(12, 19, 50)),
            TextWrapping = TextWrapping.NoWrap,
            AcceptsReturn = true
        };
        root.Children.Add(_body);

        Content = root;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        var len = _data.Length;
        var take = (int)Math.Min(MaxInitialHexBytes, len);
        _status.Text = len == 0
            ? "Пустой буфер."
            : take < len
                ? $"Всего {len} байт. Ниже — первые {take} в hex (остальное через «Сохранить в файл»)."
                : $"Всего {len} байт (hex).";

        if (len == 0)
        {
            _body.Text = "";
            return;
        }

        try
        {
            var bytes = _data.Slice(0, take).ToArray();
            var dump = await Task.Run(() => BuildHexDump(bytes)).ConfigureAwait(true);
            _body.Text = dump;
        }
        catch (Exception ex)
        {
            _body.Text = $"Ошибка построения дампа: {ex.Message}";
        }
    }

    private async Task CopyVisibleAsync()
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(_body.Text ?? "");
        }
        catch
        {
            // ignore clipboard errors
        }
    }

    private async Task SaveToFileAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить бинарные данные",
            SuggestedFileName = "payload.bin",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Binary") { Patterns = new[] { "*.bin" } },
                new FilePickerFileType("All files") { Patterns = new[] { "*.*" } },
            }
        });

        if (file == null)
            return;

        try
        {
            await using var stream = await file.OpenWriteAsync();
            await stream.WriteAsync(_data.ToArray());
        }
        catch (Exception ex)
        {
            var dialog = new Window
            {
                Title = "Сохранение",
                Width = 400,
                Height = 150,
                Content = new TextBlock { Text = ex.Message, Margin = new Thickness(16), TextWrapping = TextWrapping.Wrap }
            };
            await dialog.ShowDialog(this);
        }
    }

    private static string BuildHexDump(byte[] bytes)
    {
        const int row = 16;
        var sb = new StringBuilder(bytes.Length * 4 + 64);
        for (var off = 0; off < bytes.Length; off += row)
        {
            sb.Append(off.ToString("X8"));
            sb.Append("  ");
            var lineLen = Math.Min(row, bytes.Length - off);
            for (var i = 0; i < lineLen; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(bytes[off + i].ToString("X2"));
            }

            for (var pad = lineLen; pad < row; pad++)
                sb.Append("   ");
            sb.Append("  │ ");
            for (var i = 0; i < lineLen; i++)
            {
                var b = bytes[off + i];
                sb.Append(b is >= 32 and < 127 ? (char)b : '.');
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
