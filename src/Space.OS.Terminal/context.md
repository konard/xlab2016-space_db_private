# Space.OS.Terminal — сжатый контекст для продолжения работы

## Назначение

WPF-приложение: воркспейс, редактор `.agi` (AvalonEdit), отладчик AGI, vault, монитор исполнения. Ссылается на `Magic.Kernel`, `Magic.Kernel.Terminal`.

## Отладчик в редакторе (`MainWindow.xaml` / `MainWindow.xaml.cs`)

- Кнопки: **Continue**, **Step**, **Step into**, **Step over**, **Stop** (рядом с **Debug**).
- `InterpreterDebugSession`: **`BeginRun()`** перед `InterpreteAsync`, **`EndRun()`** в `finally`; иначе нет рабочего `ContinueCancellationToken`.
- Команды с UI: **`RequestContinue`**, **`RequestStepInstruction`**, **`RequestStepInto`** (сейчас = step instruction), **`RequestStepOverLine`**, **`RequestStop`**. Раньше был несуществующий **`Continue()`** — заменён.
- **`SetDebugToolbarPaused(true|false)`**: при паузе (`PausedAtLine`) включаются все пять кнопок; после отправки команды — выключаются до следующей паузы.
- Breakpoints: `HashSet<int> _agiBreakpoints` + margin в редакторе; копируются в `session.Breakpoints` перед запуском.

## Интерпретатор (`Magic.Kernel` / `Interpretation`)

- **`Interpreter.DebugSession`**: пауза через **`MaybeDebugPauseBeforeExecuteAsync`** (breakpoint / step-over на другую `SourceLine`), после инструкции — **`MaybeDebugPauseAfterInstructionAsync`** (step на одну инструкцию).
- **`DebugResumeAction`**: Run, StepOverLine, StepInstruction, Stop; **`ApplyDebugResumeAction`** выставляет `_debugSkipSourceLine`, `_breakOnDifferentSourceLine` / `_stepOverAnchorLine`, `_instructionStepsRemaining`.
- **Stop**: отмена токена + `ReleaseGate`; возможен **`OperationCanceledException`** → `InterpreteAsync` / `InterpreteFromEntryAsync` возвращают `Success = false`.

## Компиляция и строки исходника (`SemanticAnalyzer` / `StatementLoweringCompiler`)

- **Breakpoints на 2-й и далее строках**: раньше `FlushStatementLines` склеивал несколько строк в один `Lower` и **`SourceLine` брался только у первой** → ко второму breakpoint не попадали. Сейчас **по одной строке на вызов `Lower`**, затем проставляется `InstructionNode.SourceLine` из AST.
- **Регрессия после пофиксеного `SourceLine`**: отдельный `Lower` на строку сбрасывал **`vars`** → локальные `var x := …` не были видны на следующей строке (пример: `Cannot compile if condition: 'authentication.isAuthenticated'`). Исправление: **`_crossLineVarState`** в `StatementLoweringCompiler` + **`BeginStatementSequence()`**; в начале `Lower` мержится состояние, в конце — обновляется из `vars`; **`_localSlots` (параметры процедуры)** после этого снова накладываются и перекрывают имена.
- **`SemanticAnalyzer`**: `BeginStatementSequence()` — перед prelude; снова перед entrypoint (без протекания локалей prelude); для **каждого** `procCompiler` после `InheritGlobalSlots`; **перед каждой** function в цикле.

## Полезные пути

| Область        | Файл |
|----------------|------|
| UI отладки     | `Space.OS.Terminal/MainWindow.xaml`, `MainWindow.xaml.cs` |
| Сессия отладки | `Magic.Kernel/Interpretation/InterpreterDebugSession.cs` |
| Паузы в цикле  | `Magic.Kernel/Interpretation/Interpreter.cs` |
| Lower / vars   | `Magic.Kernel/Compilation/StatementLoweringCompiler.cs` |
| Flush + lines  | `Magic.Kernel/Compilation/SemanticAnalyzer.cs` |
| Пример claw    | `design/Space/samples/claw/client_claw.agi` |

## Сборка

```bash
dotnet build src/Space.OS.Terminal/Space.OS.Terminal.csproj
```

Дата контекста: 2026-03-27.
