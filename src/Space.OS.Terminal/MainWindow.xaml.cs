using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Magic.Kernel;
using Magic.Kernel.Interpretation;
using Magic.Kernel.Core;
using Magic.Kernel.Core.OS;
using Magic.Kernel.Runtime;
using Magic.Kernel.Terminal.Models;
using Magic.Kernel.Terminal.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpaceDb.Services;
using ICSharpCode.AvalonEdit.Rendering;

namespace Space.OS.Terminal;

public partial class MainWindow : Window
{
    private readonly TerminalWorkspaceService _workspace = new();
    private readonly TerminalMonitorService _monitor = new();
    private readonly SimulatorRunService _runService = new();
    private readonly string _appSettingsPath;
    private readonly string _spaceConfigPath;
    private readonly string _vaultPath;
    private bool _sidebarCollapsed;
    private readonly List<RuntimeExecutionRecord> _executionRecords = [];
    private IHost? _host;
    private MagicKernel? _kernel;

    private readonly List<ExecutionUnitItem> _executionUnits = [];
    private readonly List<VaultItem> _vaultItems = [];
    private readonly List<VaultStoreRow> _vaultRows = [];
    private readonly List<string> _consoleLines = [];
    private readonly DispatcherTimer _consoleTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly MagicAgiColorizer _codeColorizer = new();
    private readonly DispatcherTimer _codeHighlightTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private readonly HashSet<int> _agiBreakpoints = new();
    private InterpreterDebugSession? _activeDebugSession;
    private int _debugHighlightDocumentLine;
    private readonly AgiDebugCurrentLineRenderer _debugCurrentLineRenderer;
    private string? _codeEditorPath;

    public MainWindow()
    {
        InitializeComponent();
        _debugCurrentLineRenderer = new AgiDebugCurrentLineRenderer(() => _debugHighlightDocumentLine);
        CodeEditor.TextArea.TextView.BackgroundRenderers.Add(_debugCurrentLineRenderer);
        CodeEditor.TextArea.LeftMargins.Insert(0, new AgiBreakpointMargin(_agiBreakpoints, SyncBreakpointsToActiveDebugSession));
        CodeEditor.TextArea.TextView.LineTransformers.Add(_codeColorizer);
        CodeEditor.Options.AllowScrollBelowDocument = false;
        _codeHighlightTimer.Tick += CodeHighlightTimer_Tick;
        CodeEditor.Document.TextChanged += CodeEditor_Document_TextChanged;

        _appSettingsPath = ResolveProjectFile("src", "Space.OS.Simulator", "appsettings.json");
        _spaceConfigPath = SpaceEnvironment.GetFilePath("dev.json");
        _vaultPath = ResolveVaultPath();
        LoadAll();
        _consoleTimer.Tick += ConsoleTimer_Tick;
        _consoleTimer.Start();
        Loaded += MainWindow_Loaded;
    }

    private void LoadAll()
    {
        LoadExecutionUnits();
        LoadVaultItems();
        LoadStorageTree();
        RefreshMonitor("monitor-exes");
    }

    private static string ResolveProjectFile(params string[] parts)
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, Path.Combine(parts));
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }

            var parent = Directory.GetParent(dir);
            if (parent is null)
            {
                break;
            }

            dir = parent.FullName;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
    }

    private static string ResolveVaultPath()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "design", "Space", "vault.json");
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }

            var parent = Directory.GetParent(dir);
            if (parent is null)
            {
                break;
            }

            dir = parent.FullName;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "vault.json"));
    }

    private void LoadExecutionUnits()
    {
        _executionUnits.Clear();
        _executionUnits.AddRange(_workspace.LoadExecutionUnits(_spaceConfigPath));
        ExecutionUnitsGrid.ItemsSource = null;
        ExecutionUnitsGrid.ItemsSource = _executionUnits;
    }

    private void LoadVaultItems()
    {
        _vaultItems.Clear();
        _vaultItems.AddRange(_workspace.LoadVaultItems(_vaultPath));
        VaultItemsGrid.ItemsSource = null;
        VaultItemsGrid.ItemsSource = _vaultItems;
        if (_vaultItems.Count > 0)
        {
            VaultItemsGrid.SelectedIndex = 0;
        }
        else
        {
            _vaultRows.Clear();
            VaultStoreGrid.ItemsSource = null;
        }
    }

    private void RefreshVaultRows()
    {
        _vaultRows.Clear();
        if (VaultItemsGrid.SelectedItem is VaultItem selected)
        {
            _vaultRows.AddRange(_workspace.ToRows(selected, ShowSensitiveCheckBox.IsChecked == true));
        }

        VaultStoreGrid.ItemsSource = null;
        VaultStoreGrid.ItemsSource = _vaultRows;
    }

    private string CurrentMonitorTag()
    {
        if (NavigationTree.SelectedItem is TreeViewItem t && t.Tag is string tag && tag.StartsWith("monitor-", StringComparison.OrdinalIgnoreCase))
            return tag;
        return "monitor-exes";
    }

    private void RefreshMonitor(string tag)
    {
        var snapshot = _monitor.CreateSnapshot(_kernel, _executionRecords);
        List<MonitorRow> rows = tag switch
        {
            "monitor-streams" => snapshot.Streams,
            "monitor-drivers" => snapshot.Drivers,
            "monitor-connections" => snapshot.Connections,
            _ => snapshot.Exes
        };

        MonitorGrid.ItemsSource = null;
        MonitorGrid.ItemsSource = rows;
        MonitorStatsText.Text = $"exes={snapshot.Exes.Count}; streams={snapshot.Streams.Count}; drivers={snapshot.Drivers.Count}; connections={snapshot.Connections.Count}; devices={snapshot.TotalDevices}";
    }

    private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        _sidebarCollapsed = !_sidebarCollapsed;
        SidebarColumn.Width = _sidebarCollapsed ? new GridLength(56) : new GridLength(290);
    }

    private void NavigationTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (NavigationTree.SelectedItem is not TreeViewItem selected || selected.Tag is not string tag)
        {
            return;
        }

        ExecutionUnitsPanel.Visibility = tag == "space-executionunits" ? Visibility.Visible : Visibility.Collapsed;
        VaultPanel.Visibility = tag == "space-vault" ? Visibility.Visible : Visibility.Collapsed;
        StoragePanel.Visibility = tag == "space-storage" ? Visibility.Visible : Visibility.Collapsed;
        CodeEditorPanel.Visibility = tag == "space-code-editor" ? Visibility.Visible : Visibility.Collapsed;
        MonitorPanel.Visibility = tag.StartsWith("monitor-", StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
        ConsolePanel.Visibility = tag == "console-logs" ? Visibility.Visible : Visibility.Collapsed;

        SectionTitle.Text = selected.Header?.ToString() ?? "Space.OS.Terminal";
        SectionMeta.Text = tag switch
        {
            "space-executionunits" => _spaceConfigPath,
            "space-vault" => _vaultPath,
            "space-code-editor" => _codeEditorPath ?? string.Empty,
            _ => "monitor"
        };

        if (tag.StartsWith("monitor-", StringComparison.OrdinalIgnoreCase))
        {
            RefreshMonitor(tag);
        }
        else if (tag == "console-logs")
        {
            DrainConsole();
        }
        else if (tag == "space-storage")
        {
            LoadStorageTree();
        }
        else if (tag == "space-code-editor")
        {
            ReloadCodeEditor();
        }
    }

    private void ExecutionUnitsReload_Click(object sender, RoutedEventArgs e) => LoadExecutionUnits();

    private void ExecutionUnitsAdd_Click(object sender, RoutedEventArgs e)
    {
        _executionUnits.Add(new ExecutionUnitItem { Path = "samples/new_unit.agi", InstanceCount = 1 });
        ExecutionUnitsGrid.Items.Refresh();
    }

    private void ExecutionUnitsDelete_Click(object sender, RoutedEventArgs e)
    {
        if (ExecutionUnitsGrid.SelectedItem is ExecutionUnitItem item)
        {
            _executionUnits.Remove(item);
            ExecutionUnitsGrid.Items.Refresh();
        }
    }

    private void ExecutionUnitsSave_Click(object sender, RoutedEventArgs e)
    {
        _workspace.SaveExecutionUnits(_spaceConfigPath, _executionUnits);
        SectionMeta.Text = $"saved: {_spaceConfigPath}";
    }

    private void ExecutionUnitsRowEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not ExecutionUnitItem item)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(item.Path))
        {
            return;
        }

        var fullPath = SpaceEnvironment.GetFilePath(item.Path);
        OpenCodeEditor(fullPath);
        SelectNavigationTag("space-code-editor");
    }

    private void StorageReload_Click(object sender, RoutedEventArgs e) => LoadStorageTree();
    private void CodeEditorReload_Click(object sender, RoutedEventArgs e) => ReloadCodeEditor();

    private void CodeEditorSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_codeEditorPath))
        {
            SectionMeta.Text = "no file path";
            return;
        }

        try
        {
            File.WriteAllText(_codeEditorPath, CodeEditor.Document.Text);
            SectionMeta.Text = $"saved: {_codeEditorPath}";
        }
        catch (Exception ex)
        {
            SectionMeta.Text = $"save failed: {ex.Message}";
        }
    }

    /// <summary>Активная сессия копирует breakpoint только при старте — дублируем набор при клике по margin во время Run.</summary>
    private void SyncBreakpointsToActiveDebugSession()
    {
        _activeDebugSession?.ReplaceBreakpointsFrom(_agiBreakpoints);
    }

    private void SetDebugToolbarPaused(bool paused)
    {
        DebugContinueButton.IsEnabled = paused;
        DebugStepButton.IsEnabled = paused;
        DebugStepIntoButton.IsEnabled = paused;
        DebugStopButton.IsEnabled = paused;
    }

    private void OnInterpreterPausedAtLine(int line)
    {
        void apply()
        {
            SectionMeta.Text = $"debug: paused at line {line}";
            SetDebugToolbarPaused(true);

            _debugHighlightDocumentLine = line;
            CodeEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);

            try
            {
                if (line >= 1 && line <= CodeEditor.Document.LineCount)
                {
                    var dl = CodeEditor.Document.GetLineByNumber(line);
                    CodeEditor.TextArea.Caret.Offset = dl.Offset;
                    CodeEditor.ScrollTo(line, 1);
                    CodeEditor.TextArea.Caret.BringCaretToView();
                }
                else
                {
                    CodeEditor.ScrollToLine(line);
                }
            }
            catch
            {
                // ignore scroll/caret errors
            }

            CodeEditor.Focus();
            CodeEditor.TextArea.Focus();
        }

        if (Dispatcher.CheckAccess())
            apply();
        else
            Dispatcher.Invoke(apply);
    }

    private async void CodeEditorDebug_Click(object sender, RoutedEventArgs e)
    {
        if (_kernel == null)
        {
            MessageBox.Show(this, "Kernel not ready.", "Debug");
            return;
        }

        var source = CodeEditor.Document.Text ?? string.Empty;
        var comp = await _kernel.CompileAsync(source).ConfigureAwait(true);
        if (!comp.Success || comp.Result == null)
        {
            MessageBox.Show(this, comp.ErrorMessage ?? "Compilation failed.", "Debug");
            return;
        }

        var debugName = !string.IsNullOrWhiteSpace(_codeEditorPath)
            ? $"debug:{Path.GetFileNameWithoutExtension(_codeEditorPath)}"
            : "debug:editor";
        var debugSource = !string.IsNullOrWhiteSpace(_codeEditorPath) ? _codeEditorPath! : "(unsaved editor buffer)";

        var debugRecord = new RuntimeExecutionRecord
        {
            Name = debugName,
            SourcePath = debugSource,
            StartedAtUtc = DateTimeOffset.UtcNow,
            Status = "running",
            Success = false,
            InstanceCount = 1
        };
        _executionRecords.Add(debugRecord);
        RefreshMonitor(CurrentMonitorTag());

        var session = new InterpreterDebugSession();
        session.ReplaceBreakpointsFrom(_agiBreakpoints);

        session.PausedAtLine += OnInterpreterPausedAtLine;
        _activeDebugSession = session;
        session.BeginRun();

        var interpreter = new Interpreter
        {
            Configuration = _kernel.Configuration,
            DebugSession = session,
            BypassRuntimeSpawn = true
        };

        SetDebugToolbarPaused(false);
        SectionMeta.Text = "debug: running…";

        InterpretationResult? runResult = null;
        try
        {
            runResult = await Task.Run(async () => await interpreter.InterpreteAsync(comp.Result!).ConfigureAwait(false))
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                debugRecord.Success = false;
                debugRecord.Status = "error";
                debugRecord.EndedAtUtc = DateTimeOffset.UtcNow;
                debugRecord.ErrorMessage = ex.Message;
                RefreshMonitor(CurrentMonitorTag());
                SectionMeta.Text = $"debug: error {ex.Message}";
                MessageBox.Show(this, ex.Message, "Debug");
            });
        }
        finally
        {
            session.PausedAtLine -= OnInterpreterPausedAtLine;
            session.EndRun();
            _activeDebugSession = null;
            Dispatcher.Invoke(() =>
            {
                _debugHighlightDocumentLine = 0;
                CodeEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
                SetDebugToolbarPaused(false);
                if (!debugRecord.EndedAtUtc.HasValue)
                {
                    debugRecord.Success = runResult?.Success ?? false;
                    debugRecord.Status = debugRecord.Success ? "stopped" : "error";
                    debugRecord.EndedAtUtc = DateTimeOffset.UtcNow;
                    if (!debugRecord.Success && string.IsNullOrWhiteSpace(debugRecord.ErrorMessage))
                        debugRecord.ErrorMessage = "Interpretation returned failure.";
                    RefreshMonitor(CurrentMonitorTag());
                }

                SectionMeta.Text = runResult?.Success == true ? "debug: finished" : "debug: failed or interrupted";
            });
        }
    }

    private void DebugContinue_Click(object sender, RoutedEventArgs e)
    {
        _debugHighlightDocumentLine = 0;
        CodeEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
        _activeDebugSession?.RequestContinue();
        SetDebugToolbarPaused(false);
    }

    /// <summary>Следующая строка исходника (раньше была одна инструкция — на одной строке казалось, что «шаг» не работает).</summary>
    private void DebugStep_Click(object sender, RoutedEventArgs e)
    {
        _debugHighlightDocumentLine = 0;
        CodeEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
        _activeDebugSession?.RequestStepOverLine();
        SetDebugToolbarPaused(false);
    }

    private void DebugStepInto_Click(object sender, RoutedEventArgs e)
    {
        _debugHighlightDocumentLine = 0;
        CodeEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
        _activeDebugSession?.RequestStepInstruction();
        SetDebugToolbarPaused(false);
    }

    private void DebugStop_Click(object sender, RoutedEventArgs e)
    {
        _activeDebugSession?.RequestStop();
        SetDebugToolbarPaused(false);
    }

    private void CodeEditor_Document_TextChanged(object? sender, EventArgs e)
    {
        _codeHighlightTimer.Stop();
        _codeHighlightTimer.Start();
    }

    private void CodeHighlightTimer_Tick(object? sender, EventArgs e)
    {
        _codeHighlightTimer.Stop();
        _codeColorizer.DocumentUpdated(CodeEditor.Document);
        CodeEditor.TextArea.TextView.Redraw();
    }

    private void VaultReload_Click(object sender, RoutedEventArgs e) => LoadVaultItems();

    private void VaultAdd_Click(object sender, RoutedEventArgs e)
    {
        _vaultItems.Add(new VaultItem { Program = "program", System = "system", Module = "module", InstanceIndex = 0 });
        VaultItemsGrid.Items.Refresh();
    }

    private void VaultDelete_Click(object sender, RoutedEventArgs e)
    {
        if (VaultItemsGrid.SelectedItem is VaultItem item)
        {
            _vaultItems.Remove(item);
            VaultItemsGrid.Items.Refresh();
            RefreshVaultRows();
        }
    }

    private void VaultSave_Click(object sender, RoutedEventArgs e)
    {
        if (VaultItemsGrid.SelectedItem is VaultItem selected)
        {
            _workspace.ApplyRows(selected, _vaultRows);
        }

        _workspace.SaveVaultItems(_vaultItems, _vaultPath);
        SectionMeta.Text = $"saved: {_vaultPath}";
    }

    private void VaultItemsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshVaultRows();

    private void ShowSensitiveChanged(object sender, RoutedEventArgs e) => RefreshVaultRows();

    private void MonitorRefresh_Click(object sender, RoutedEventArgs e)
    {
        var tag = (NavigationTree.SelectedItem as TreeViewItem)?.Tag?.ToString() ?? "monitor-exes";
        RefreshMonitor(tag);
    }

    private void ConsoleTimer_Tick(object? sender, EventArgs e) => DrainConsole();

    private void ConsoleRefresh_Click(object sender, RoutedEventArgs e) => DrainConsole();

    private void ConsoleClear_Click(object sender, RoutedEventArgs e)
    {
        _consoleLines.Clear();
        ConsoleLogsTextBox.Clear();
    }

    private void DrainConsole()
    {
        var drained = ConsoleLogCapture.Drain();
        if (drained.Count == 0)
        {
            return;
        }

        var trimmedHead = false;
        foreach (var line in drained)
        {
            _consoleLines.Add(line);
        }

        while (_consoleLines.Count > 2500)
        {
            _consoleLines.RemoveAt(0);
            trimmedHead = true;
        }

        if (trimmedHead)
        {
            ConsoleLogsTextBox.Text = string.Join(Environment.NewLine, _consoleLines);
        }
        else
        {
            foreach (var line in drained)
            {
                if (ConsoleLogsTextBox.Text.Length > 0)
                    ConsoleLogsTextBox.AppendText(Environment.NewLine);
                ConsoleLogsTextBox.AppendText(line);
            }
        }

        ConsoleLogsTextBox.CaretIndex = ConsoleLogsTextBox.Text.Length;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_Loaded;
        await BootstrapAndRunInitialAsync();
    }

    private async Task BootstrapAndRunInitialAsync()
    {
        try
        {
            OSSystem.StartKernel();
            var builder = Host.CreateApplicationBuilder([]);
            builder.Configuration.AddJsonFile(_appSettingsPath, optional: true, reloadOnChange: true);
            builder.Configuration.AddEnvironmentVariables();
            builder.Services.AddSingleton<MagicKernel>();
            builder.Services.AddRocksDb(builder.Configuration);

            _host = builder.Build();
            using var scope = _host.Services.CreateScope();
            _kernel = scope.ServiceProvider.GetRequiredService<MagicKernel>();
            var defaultDisk = scope.ServiceProvider.GetRequiredService<RocksDbSpaceDisk>();
            _kernel.Devices.Add(defaultDisk);
            await _kernel.StartKernel();

            var executionUnits = SpaceEnvironment.ExecutionUnits;
            if (executionUnits.Count == 0)
            {
                SectionMeta.Text = "kernel started; no executionUnits in Space/dev.json";
                RefreshMonitor("monitor-exes");
                return;
            }

            foreach (var unit in executionUnits)
            {
                var record = new RuntimeExecutionRecord
                {
                    Name = Path.GetFileNameWithoutExtension(unit.Path) ?? unit.Path,
                    SourcePath = SpaceEnvironment.GetFilePath(unit.Path),
                    StartedAtUtc = DateTimeOffset.UtcNow,
                    Status = "starting",
                    Success = false,
                    InstanceCount = unit.InstanceCount
                };
                _executionRecords.Add(record);

                _ = RunUnitInBackgroundAsync(record, unit);
            }

            SectionMeta.Text = $"startup executionUnits launched: {executionUnits.Count}";
        }
        catch (Exception ex)
        {
            _executionRecords.Add(new RuntimeExecutionRecord
            {
                Name = "startup-executionUnits",
                SourcePath = "Space/dev.json:executionUnits",
                StartedAtUtc = DateTimeOffset.UtcNow,
                Status = "error",
                Success = false,
                InstanceCount = 0,
                EndedAtUtc = DateTimeOffset.UtcNow,
                ErrorMessage = ex.Message
            });
            SectionMeta.Text = $"startup error: {ex.Message}";
            Debug.WriteLine(ex);
        }
        finally
        {
            RefreshMonitor("monitor-exes");
        }
    }

    private async Task RunUnitInBackgroundAsync(RuntimeExecutionRecord record, SpaceEnvironment.ExecutionUnitConfig unit)
    {
        await Dispatcher.InvokeAsync(() =>
        {
            record.Status = "running";
            RefreshMonitor("monitor-exes");
        });

        try
        {
            var host = new ExecutionUnitHost(_kernel!);
            var ok = await _runService.RunConfiguredUnitAsync(host, unit).ConfigureAwait(false);
            await Dispatcher.InvokeAsync(() =>
            {
                record.Success = ok;
                record.Status = ok ? "stopped" : "error";
                record.EndedAtUtc = DateTimeOffset.UtcNow;
                if (!ok && string.IsNullOrWhiteSpace(record.ErrorMessage))
                {
                    record.ErrorMessage = "Execution returned false.";
                }

                RefreshMonitor("monitor-exes");
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                record.Success = false;
                record.Status = "error";
                record.EndedAtUtc = DateTimeOffset.UtcNow;
                record.ErrorMessage = ex.Message;
                RefreshMonitor("monitor-exes");
            });
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _consoleTimer.Stop();
        _host?.Dispose();
    }

    private void LoadStorageTree()
    {
        StorageTree.Items.Clear();
        var rootPath = SpaceEnvironment.Path;
        if (!Directory.Exists(rootPath))
        {
            StorageTree.Items.Add(new TreeViewItem { Header = $"SPACE_PATH not found: {rootPath}" });
            return;
        }

        var root = BuildAgiTree(rootPath);
        StorageTree.Items.Add(root);
        root.IsExpanded = true;
    }

    private TreeViewItem BuildAgiTree(string directory)
    {
        var dirInfo = new DirectoryInfo(directory);
        var rootNode = new TreeViewItem { Header = dirInfo.Name, Tag = dirInfo.FullName };

        foreach (var subDir in dirInfo.GetDirectories().OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var child = BuildAgiTree(subDir.FullName);
            if (child.Items.Count > 0)
            {
                rootNode.Items.Add(child);
            }
        }

        foreach (var file in dirInfo.GetFiles("*.agi").OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            rootNode.Items.Add(CreateAgiFileNode(file.FullName));
        }

        return rootNode;
    }

    private TreeViewItem CreateAgiFileNode(string fullPath)
    {
        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };
        stack.Children.Add(new TextBlock
        {
            Text = Path.GetFileName(fullPath),
            VerticalAlignment = VerticalAlignment.Center
        });
        var button = new Button
        {
            Content = "Edit",
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(8, 2, 8, 2),
            Tag = fullPath,
            FontSize = 11
        };
        button.Click += StorageEdit_Click;
        stack.Children.Add(button);

        return new TreeViewItem
        {
            Header = stack,
            Tag = fullPath
        };
    }

    private void StorageEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string path)
        {
            return;
        }

        OpenCodeEditor(path);
        SelectNavigationTag("space-code-editor");
    }

    private void SelectNavigationTag(string tag)
    {
        foreach (var item in NavigationTree.Items)
        {
            if (item is TreeViewItem root && TrySelectByTag(root, tag))
            {
                return;
            }
        }
    }

    private static bool TrySelectByTag(TreeViewItem node, string tag)
    {
        if (string.Equals(node.Tag as string, tag, StringComparison.OrdinalIgnoreCase))
        {
            node.IsSelected = true;
            return true;
        }

        foreach (var child in node.Items)
        {
            if (child is TreeViewItem childNode)
            {
                if (TrySelectByTag(childNode, tag))
                {
                    node.IsExpanded = true;
                    return true;
                }
            }
        }

        return false;
    }

    private void OpenCodeEditor(string path)
    {
        _codeEditorPath = path;
        ReloadCodeEditor();
    }

    private void ReloadCodeEditor()
    {
        if (string.IsNullOrWhiteSpace(_codeEditorPath))
        {
            CodeEditorFileText.Text = "No file selected";
            CodeEditor.Document.Text = string.Empty;
            _codeColorizer.DocumentUpdated(CodeEditor.Document);
            CodeEditor.TextArea.TextView.Redraw();
            return;
        }

        if (!File.Exists(_codeEditorPath))
        {
            CodeEditorFileText.Text = $"Not found: {_codeEditorPath}";
            CodeEditor.Document.Text = string.Empty;
            _codeColorizer.DocumentUpdated(CodeEditor.Document);
            CodeEditor.TextArea.TextView.Redraw();
            return;
        }

        CodeEditorFileText.Text = _codeEditorPath;
        CodeEditor.Document.Text = File.ReadAllText(_codeEditorPath);
        _codeColorizer.DocumentUpdated(CodeEditor.Document);
        CodeEditor.TextArea.TextView.Redraw();
    }
}