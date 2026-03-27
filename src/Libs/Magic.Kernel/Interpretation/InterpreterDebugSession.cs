using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Magic.Kernel.Interpretation;

public enum DebugResumeAction
{
    Run,
    /// <summary>Следующая другая строка исходника (SourceLine &gt; 0).</summary>
    StepOverLine,
    /// <summary>Одна машинная инструкция (входит в call).</summary>
    StepInstruction,
    Stop
}

/// <summary>Паузы отладчика AGI (1-based номера строк).</summary>
public sealed class InterpreterDebugSession
{
    /// <summary>
    /// Одно ожидание = один TCS: повторный сигнал на уже завершённый wait ничего не делает.
    /// У <see cref="SemaphoreSlim"/> лишний Release давал «orphan permit» — следующий WaitAsync сразу проходил,
    /// <see cref="Interpreter"/> вызывал Continue без клика и подавлял breakpoint через <c>_debugSkipSourceLine</c>.
    /// </summary>
    private TaskCompletionSource<bool>? _continueTcs;

    private CancellationTokenSource? _stopCts;
    private DebugResumeAction _pendingResume = DebugResumeAction.Run;

    private readonly object _breakpointLock = new();
    private readonly HashSet<int> _breakpoints = new();

    public event Action<int>? PausedAtLine;

    /// <summary>Копирует набор строк breakpoint (1-based). Потокобезопасно: интерпретатор читает из фона.</summary>
    public void ReplaceBreakpointsFrom(IEnumerable<int> sourceLines)
    {
        lock (_breakpointLock)
        {
            _breakpoints.Clear();
            if (sourceLines == null)
                return;
            foreach (var line in sourceLines)
            {
                if (line > 0)
                    _breakpoints.Add(line);
            }
        }
    }

    public void BeginRun(CancellationToken externalToken = default)
    {
        _pendingResume = DebugResumeAction.Run;
        _continueTcs = null;

        _stopCts?.Dispose();
        _stopCts = externalToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(externalToken)
            : new CancellationTokenSource();
    }

    public CancellationToken ContinueCancellationToken => _stopCts?.Token ?? CancellationToken.None;

    public void EndRun()
    {
        try
        {
            _stopCts?.Cancel();
        }
        catch
        {
            // ignore
        }

        ReleaseWaitCore();

        _stopCts?.Dispose();
        _stopCts = null;

        _pendingResume = DebugResumeAction.Run;
    }

    public bool IsBreakpointLine(int sourceLine)
    {
        lock (_breakpointLock)
            return sourceLine > 0 && _breakpoints.Contains(sourceLine);
    }

    public async Task WaitPausedAsync(int line, CancellationToken cancellationToken = default)
    {
        PausedAtLine?.Invoke(line);
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _continueTcs = tcs;

        using (cancellationToken.Register(() => tcs.TrySetResult(true)))
        {
            await tcs.Task.ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    public DebugResumeAction ConsumeResumeAction()
    {
        var a = _pendingResume;
        _pendingResume = DebugResumeAction.Run;
        return a;
    }

    public void RequestContinue()
    {
        _pendingResume = DebugResumeAction.Run;
        ReleaseWaitCore();
    }

    public void RequestStepOverLine()
    {
        _pendingResume = DebugResumeAction.StepOverLine;
        ReleaseWaitCore();
    }

    public void RequestStepInstruction()
    {
        _pendingResume = DebugResumeAction.StepInstruction;
        ReleaseWaitCore();
    }

    /// <summary>F11 — сейчас то же, что одна инструкция.</summary>
    public void RequestStepInto() => RequestStepInstruction();

    public void RequestStop()
    {
        _pendingResume = DebugResumeAction.Stop;
        try
        {
            _stopCts?.Cancel();
        }
        catch
        {
            // ignore
        }

        ReleaseWaitCore();
    }

    private void ReleaseWaitCore()
        => _continueTcs?.TrySetResult(true);
}
