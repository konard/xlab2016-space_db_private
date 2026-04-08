using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Magic.Kernel;
using Magic.Kernel.Compilation;
using Magic.Kernel.Processor;
using Magic.Kernel2.Compilation2.Ast2;

namespace Magic.Kernel2.Compilation2
{
    /// <summary>
    /// Magic compiler 2.0 assembler.
    /// <para>
    /// Walks the <strong>fully-typed AST</strong> produced by <see cref="Parser2"/> and
    /// generates <see cref="Command"/> bytecode directly from typed AST nodes.
    /// No <c>StatementLoweringCompiler</c>, no re-parsing of raw text, no deferred lowering.
    /// </para>
    /// </summary>
    public class Assembler2
    {
        /// <summary>
        /// Assemble a complete program from its typed AST and symbol table.
        /// This is the main entry point called by <see cref="Compiler2"/>.
        /// Pipeline: typed AST → <see cref="ExecutableUnit"/> with all procedures, functions, and entrypoint.
        /// </summary>
        public ExecutableUnit Assemble(ProgramNode2 program, SymbolTable2 symbolTable)
        {
            var unit = new ExecutableUnit
            {
                Version = program.Version,
                Name = program.ProgramName,
                Module = program.Module,
                System = program.System
            };

            // Assemble procedures.
            foreach (var proc in program.Procedures)
            {
                var procedure = AssembleProcedure(proc, symbolTable);
                unit.Procedures[proc.Name] = procedure;
            }

            // Assemble functions.
            foreach (var func in program.Functions)
            {
                var function = AssembleFunction(func, symbolTable);
                unit.Functions[func.Name] = function;
            }

            // Assemble nested procedures/functions discovered during procedure body assembly.
            foreach (var nested in symbolTable.NestedProcedures)
            {
                var nestedDecl = new ProcedureDeclarationNode2
                {
                    Name = nested.Name,
                    Parameters = nested.Parameters,
                    Body = nested.Body
                };
                var nestedProc = AssembleProcedure(nestedDecl, symbolTable);
                unit.Procedures[nested.Name] = nestedProc;
            }
            foreach (var nested in symbolTable.NestedFunctions)
            {
                var nestedDecl = new FunctionDeclarationNode2
                {
                    Name = nested.Name,
                    Parameters = nested.Parameters,
                    Body = nested.Body
                };
                var nestedFunc = AssembleFunction(nestedDecl, symbolTable);
                unit.Functions[nested.Name] = nestedFunc;
            }

            // Assemble entrypoint.
            // The entrypoint runs: type declarations first (allocating slots 0..N),
            // then the entrypoint body statements.
            var entryScope = symbolTable.GlobalScope;
            var entryBlock = new ExecutionBlock();

            // Emit type declarations into the entrypoint (they allocate global slots 0..N).
            foreach (var typeDecl in program.TypeDeclarations)
            {
                var typeCommands = EmitTypeDeclaration(typeDecl, entryScope);
                entryBlock.AddRange(typeCommands);
            }

            // Emit entrypoint body statements.
            if (program.EntryPoint != null)
            {
                foreach (var stmt in program.EntryPoint.Statements)
                    EmitStatement(stmt, entryScope, entryBlock, isProcedure: false);
            }

            unit.EntryPoint = entryBlock;
            return unit;
        }

        // ─── Procedure / Function assembly ───────────────────────────────────────

        private Magic.Kernel.Processor.Procedure AssembleProcedure(
            ProcedureDeclarationNode2 proc,
            SymbolTable2 symbolTable)
        {
            var procedure = new Magic.Kernel.Processor.Procedure { Name = proc.Name };

            // Create a fresh scope with access to global slots.
            // Use CreateProcedureScope so global type slots (Db>, Message<>, etc.) are resolvable.
            var scope = symbolTable.CreateProcedureScope(proc.Name, new System.Collections.Generic.List<ParameterDeclaration2>());

            // Bind parameters: V1 convention is Pop (arity), then Pop [slot] for each param in reverse.
            if (proc.Parameters.Count > 0)
            {
                procedure.Body.Add(Emit(Opcodes.Pop, 0));
                for (var pi = proc.Parameters.Count - 1; pi >= 0; pi--)
                {
                    var slot = scope.AllocateLocal(proc.Parameters[pi].Name);
                    procedure.Body.Add(PopSlot(slot, 0));
                }
            }

            // Emit procedure body — collect nested procedures/functions first.
            EmitBodyWithNested(proc.Body, scope, procedure.Body, isProcedure: true, symbolTable, procedure.Name);

            // Ensure Ret at end.
            if (procedure.Body.Count == 0 || procedure.Body[^1].Opcode != Opcodes.Ret)
                procedure.Body.Add(Emit(Opcodes.Ret, 0));

            return procedure;
        }

        private Magic.Kernel.Processor.Function AssembleFunction(
            FunctionDeclarationNode2 func,
            SymbolTable2 symbolTable)
        {
            var function = new Magic.Kernel.Processor.Function { Name = func.Name };
            var scope = symbolTable.CreateFunctionScope(func.Name, new System.Collections.Generic.List<ParameterDeclaration2>());

            if (func.Parameters.Count > 0)
            {
                function.Body.Add(Emit(Opcodes.Pop, 0));
                for (var pi = func.Parameters.Count - 1; pi >= 0; pi--)
                {
                    var slot = scope.AllocateLocal(func.Parameters[pi].Name);
                    function.Body.Add(PopSlot(slot, 0));
                }
            }

            EmitBodyWithNested(func.Body, scope, function.Body, isProcedure: false, symbolTable, func.Name);

            if (function.Body.Count == 0 || function.Body[^1].Opcode != Opcodes.Ret)
                function.Body.Add(Emit(Opcodes.Ret, 0));

            return function;
        }

        /// <summary>
        /// Emit body statements, extracting nested procedures/functions into the symbol table's
        /// extra collection (they are emitted as procedures/functions at the program level by V1).
        /// </summary>
        private void EmitBodyWithNested(
            BlockNode2 body,
            ScopeSymbols2 scope,
            ExecutionBlock target,
            bool isProcedure,
            SymbolTable2 symbolTable,
            string parentName)
        {
            foreach (var stmt in body.Statements)
            {
                if (stmt is NestedProcedureStatement2 nestedProc)
                {
                    // Nested procedures become top-level procedures in the unit.
                    symbolTable.AddNestedProcedure(nestedProc);
                    // No bytecode emitted inline for the declaration itself.
                    continue;
                }
                if (stmt is NestedFunctionStatement2 nestedFunc)
                {
                    symbolTable.AddNestedFunction(nestedFunc);
                    continue;
                }

                EmitStatement(stmt, scope, target, isProcedure);
            }
        }

        // ─── Type declaration ─────────────────────────────────────────────────────

        /// <summary>Emit bytecode for a type declaration into the given scope.</summary>
        public List<Command> EmitTypeDeclaration(TypeDeclarationNode2 typeDecl, ScopeSymbols2 scope)
        {
            var commands = new List<Command>();
            EmitTypeDeclarationInto(typeDecl, scope, commands);
            return commands;
        }

        private void EmitTypeDeclarationInto(TypeDeclarationNode2 typeDecl, ScopeSymbols2 scope, List<Command> commands)
        {
            if (typeDecl.IsTable)
                EmitTableDeclaration(typeDecl, scope, commands);
            else if (typeDecl.IsDatabase)
                EmitDatabaseDeclaration(typeDecl, scope, commands);
            else
                EmitSimpleTypeDeclaration(typeDecl, scope, commands);
        }

        private static void EmitSimpleTypeDeclaration(TypeDeclarationNode2 typeDecl, ScopeSymbols2 scope, List<Command> commands)
        {
            // push typeName; push 1 (or base count); def; pop [slot]
            // For simple types: push typeName; def; pop [slot]
            commands.Add(PushString(typeDecl.Name, typeDecl.SourceLine));
            commands.Add(PushInt(1L, typeDecl.SourceLine));
            commands.Add(Emit(Opcodes.Def, typeDecl.SourceLine));
            var slot = scope.AllocateLocal(typeDecl.Name);
            commands.Add(PopSlot(slot, typeDecl.SourceLine));
        }

        private static void EmitTableDeclaration(TypeDeclarationNode2 typeDecl, ScopeSymbols2 scope, List<Command> commands)
        {
            var slot = scope.AllocateLocal(typeDecl.Name);

            // push tableName; push "table"; push 2; def; pop [slot]
            commands.Add(PushString(typeDecl.Name, typeDecl.SourceLine));
            commands.Add(PushString("table", typeDecl.SourceLine));
            commands.Add(PushInt(2L, typeDecl.SourceLine));
            commands.Add(Emit(Opcodes.Def, typeDecl.SourceLine));
            commands.Add(PopSlot(slot, typeDecl.SourceLine));

            // Each field becomes: push [slot]; push colName; push colType; push modifiers...; push "column"; push arity; def; pop [slot]
            foreach (var field in typeDecl.Fields)
            {
                if (!TryParseColumnSpec(field.Name, field.TypeSpec ?? "", out var colName, out var colType, out var modifiers, out var isRelation, out var isArray))
                    continue;

                commands.Add(PushSlot(slot, typeDecl.SourceLine));
                commands.Add(PushString(colName, typeDecl.SourceLine));
                commands.Add(PushString(colType, typeDecl.SourceLine));

                if (isRelation)
                {
                    commands.Add(PushString(isArray ? "array" : "single", typeDecl.SourceLine));
                    commands.Add(PushString("relation", typeDecl.SourceLine));
                    commands.Add(PushInt(5L, typeDecl.SourceLine));
                }
                else
                {
                    foreach (var mod in modifiers)
                        commands.Add(PushString(mod, typeDecl.SourceLine));
                    commands.Add(PushString("column", typeDecl.SourceLine));
                    commands.Add(PushInt(4L + modifiers.Count, typeDecl.SourceLine));
                }

                commands.Add(Emit(Opcodes.Def, typeDecl.SourceLine));
                commands.Add(PopSlot(slot, typeDecl.SourceLine));
            }
        }

        private static void EmitDatabaseDeclaration(TypeDeclarationNode2 typeDecl, ScopeSymbols2 scope, List<Command> commands)
        {
            var slot = scope.AllocateLocal(typeDecl.Name);

            // push dbName; push "database"; push 2; def; pop [slot]
            commands.Add(PushString(typeDecl.Name, typeDecl.SourceLine));
            commands.Add(PushString("database", typeDecl.SourceLine));
            commands.Add(PushInt(2L, typeDecl.SourceLine));
            commands.Add(Emit(Opcodes.Def, typeDecl.SourceLine));
            commands.Add(PopSlot(slot, typeDecl.SourceLine));

            // Each referenced table: push [dbSlot]; push [tableSlot]; push "table"; push 3; def; pop [dbSlot]
            foreach (var field in typeDecl.Fields)
            {
                var tableRef = (field.TypeSpec ?? field.Name).Trim();
                if (string.IsNullOrWhiteSpace(tableRef))
                    tableRef = field.Name;

                if (!scope.TryResolveSlot(tableRef, out var tableSlot, out _))
                    continue;

                commands.Add(PushSlot(slot, typeDecl.SourceLine));
                commands.Add(PushSlot(tableSlot, typeDecl.SourceLine));
                commands.Add(PushString("table", typeDecl.SourceLine));
                commands.Add(PushInt(3L, typeDecl.SourceLine));
                commands.Add(Emit(Opcodes.Def, typeDecl.SourceLine));
                commands.Add(PopSlot(slot, typeDecl.SourceLine));
            }
        }

        // ─── Column spec parsing (table column modifiers) ─────────────────────────

        private static bool TryParseColumnSpec(
            string fieldName,
            string typeSpec,
            out string columnName,
            out string columnType,
            out List<string> modifiers,
            out bool isRelation,
            out bool isArray)
        {
            columnName = fieldName.Trim();
            columnType = "";
            modifiers = new List<string>();
            isRelation = false;
            isArray = false;

            var spec = typeSpec.Trim();
            if (string.IsNullOrWhiteSpace(columnName) || string.IsNullOrWhiteSpace(spec))
                return false;

            // Try relation spec: single identifier that references another table type, or T[]
            if (TryParseRelationSpec(spec, out var relationType, out var relationIsArray))
            {
                columnType = relationType;
                isRelation = true;
                isArray = relationIsArray;
                return true;
            }

            // nullable?
            var nullable = spec.EndsWith("?", StringComparison.Ordinal);
            if (nullable)
                spec = spec.Substring(0, spec.Length - 1).Trim();

            // length modifier: nvarchar(250)
            var lengthMatch = Regex.Match(spec, @"^(?<type>[A-Za-z_][A-Za-z0-9_]*)\((?<len>\d+)\)\s*(?<rest>.*)$", RegexOptions.IgnoreCase);
            if (lengthMatch.Success)
            {
                columnType = lengthMatch.Groups["type"].Value.Trim().ToLowerInvariant();
                modifiers.Add($"length:{lengthMatch.Groups["len"].Value.Trim()}");
                spec = lengthMatch.Groups["rest"].Value.Trim();
            }
            else
            {
                var parts = spec.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    return false;
                columnType = parts[0].Trim().ToLowerInvariant();
                spec = string.Join(" ", parts.Skip(1)).Trim();
            }

            // Support nullable marker directly on type token, e.g. "int? index".
            if (columnType.EndsWith("?", StringComparison.Ordinal))
            {
                nullable = true;
                columnType = columnType.Substring(0, columnType.Length - 1).Trim();
            }

            if (string.IsNullOrWhiteSpace(columnType))
                return false;

            if (Regex.IsMatch(spec, @"\bprimary\s+key\b", RegexOptions.IgnoreCase))
            {
                modifiers.Add("primary key");
                spec = Regex.Replace(spec, @"\bprimary\s+key\b", "", RegexOptions.IgnoreCase).Trim();
            }
            if (Regex.IsMatch(spec, @"\bidentity\b", RegexOptions.IgnoreCase))
            {
                modifiers.Add("identity");
                spec = Regex.Replace(spec, @"\bidentity\b", "", RegexOptions.IgnoreCase).Trim();
            }
            if (!string.IsNullOrWhiteSpace(spec))
                modifiers.Add(spec);

            modifiers.Add(nullable ? "nullable:1" : "nullable:0");
            return true;
        }

        private static readonly HashSet<string> BuiltinSqlTypeNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "bigint", "int", "integer", "smallint", "datetime", "timestamp", "date", "time",
            "bool", "boolean", "decimal", "double", "float", "uuid", "json", "jsonb", "text",
            "varchar", "nvarchar", "string", "any", "void", "object"
        };

        private static bool TryParseRelationSpec(string spec, out string referencedTableType, out bool isArray)
        {
            referencedTableType = "";
            isArray = false;

            var s = (spec ?? "").Trim();
            if (s.Length == 0)
                return false;

            if (s.EndsWith("?", StringComparison.Ordinal))
                s = s.Substring(0, s.Length - 1).Trim();

            if (s.EndsWith("[]", StringComparison.Ordinal))
            {
                isArray = true;
                s = s.Substring(0, s.Length - 2).Trim();
            }

            if (!Regex.IsMatch(s, @"^[A-Za-z_][A-Za-z0-9_]*(?:<>|>)?$"))
                return false;

            var normalizedType = s.Trim().ToLowerInvariant();
            if (BuiltinSqlTypeNames.Contains(normalizedType))
                return false;

            referencedTableType = s;
            return true;
        }

        // ─── Block / Statement emission ───────────────────────────────────────────

        /// <summary>
        /// Emit bytecode for a block of statements by walking each typed AST node directly.
        /// This is the v2.0 equivalent of v1.0's <c>StatementLoweringCompiler.Lower()</c>,
        /// but operates on fully-typed AST nodes instead of raw text strings.
        /// </summary>
        public ExecutionBlock EmitBlock(BlockNode2 block, ScopeSymbols2 scope, bool isProcedure)
        {
            var result = new ExecutionBlock();
            foreach (var stmt in block.Statements)
                EmitStatement(stmt, scope, result, isProcedure);
            return result;
        }

        private void EmitStatement(StatementNode2 stmt, ScopeSymbols2 scope, ExecutionBlock result, bool isProcedure)
        {
            switch (stmt)
            {
                case VarDeclarationStatement2 varDecl:
                    EmitVarDeclaration(varDecl, scope, result);
                    break;

                case AssignmentStatement2 assign:
                    EmitAssignment(assign, scope, result);
                    break;

                case CompoundAssignmentStatement2 compound:
                    EmitCompoundAssignment(compound, scope, result);
                    break;

                case CallStatement2 call:
                    EmitCallStatement(call, scope, result);
                    break;

                case StreamWaitCallStatement2 swCall:
                    EmitStreamWaitCallStatement(swCall, scope, result);
                    break;

                case ReturnStatement2 ret:
                    EmitReturnStatement(ret, scope, result);
                    break;

                case IfStatement2 ifStmt:
                    EmitIfStatement(ifStmt, scope, result, isProcedure);
                    break;

                case SwitchStatement2 switchStmt:
                    EmitSwitchStatement(switchStmt, scope, result, isProcedure);
                    break;

                case StreamWaitByDeltaLoop2 swDelta:
                    EmitStreamWaitByDeltaLoop(swDelta, scope, result, isProcedure);
                    break;

                case StreamWaitForLoop2 forLoop:
                    EmitStreamWaitForLoop(forLoop, scope, result, isProcedure);
                    break;

                case InstructionStatement2 instr:
                    EmitInstructionStatement(instr, scope, result);
                    break;

                case NestedProcedureStatement2 nested:
                    // Nested procedures are handled at the Assembler2 level — no inline bytecode.
                    result.Add(Emit(Opcodes.Nop, nested.SourceLine));
                    break;

                case NestedFunctionStatement2 nested:
                    result.Add(Emit(Opcodes.Nop, nested.SourceLine));
                    break;
            }
        }

        // ─── Variable declaration ─────────────────────────────────────────────────

        private void EmitVarDeclaration(VarDeclarationStatement2 varDecl, ScopeSymbols2 scope, ExecutionBlock result)
        {
            if (varDecl.Initializer != null)
            {
                // V1 pattern for generic types: emit expression FIRST (allocating internal temps),
                // then allocate the local slot AFTER. This preserves V1's slot ordering.
                if (varDecl.Initializer is GenericTypeExpression2)
                {
                    // For generic types: emit expression (which allocs internal temps), then alloc local.
                    EmitExpression(varDecl.Initializer, scope, result);
                    var slot = scope.AllocateLocal(varDecl.VariableName);
                    result.Add(PopSlot(slot, varDecl.SourceLine));
                }
                else if (varDecl.Initializer is VariableExpression2 ve && IsBuiltinTypeName(ve.Name))
                {
                    // Simple builtin type (vault, stream, database, etc.) — V1 pattern:
                    // push <type>; push 1; def; pop [slot]
                    result.Add(PushIdentifier(ve.Name, varDecl.SourceLine));
                    result.Add(PushInt(1L, varDecl.SourceLine));
                    result.Add(Emit(Opcodes.Def, varDecl.SourceLine));
                    var slot = scope.AllocateLocal(varDecl.VariableName);
                    result.Add(PopSlot(slot, varDecl.SourceLine));
                }
                else
                {
                    // For all other expressions: allocate local first (standard pattern).
                    var slot = scope.AllocateLocal(varDecl.VariableName);
                    EmitExpression(varDecl.Initializer, scope, result);
                    result.Add(PopSlot(slot, varDecl.SourceLine));
                }
            }
            else if (!string.IsNullOrEmpty(varDecl.ExplicitType))
            {
                // var x: Type — emit type def instruction.
                var slot = scope.AllocateLocal(varDecl.VariableName);
                result.Add(PushString(varDecl.ExplicitType, varDecl.SourceLine));
                result.Add(Emit(Opcodes.Def, varDecl.SourceLine));
                result.Add(PopSlot(slot, varDecl.SourceLine));
            }
            else
            {
                // No initializer, no explicit type — just allocate the slot.
                scope.AllocateLocal(varDecl.VariableName);
            }
        }

        private static readonly HashSet<string> BuiltinTypeNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "vault", "stream", "database", "messenger", "network", "file", "telegram", "postgres"
        };

        private static bool IsBuiltinTypeName(string name) => BuiltinTypeNames.Contains(name);

        // ─── Assignment ───────────────────────────────────────────────────────────

        private void EmitAssignment(AssignmentStatement2 assign, ScopeSymbols2 scope, ExecutionBlock result)
        {
            if (assign.Target is VariableExpression2 varExpr)
            {
                // Simple variable assignment: eval value, pop to slot.
                EmitExpression(assign.Value, scope, result);
                if (scope.TryResolveSlot(varExpr.Name, out var slot, out _))
                {
                    result.Add(PopSlot(slot, assign.SourceLine));
                }
                else
                {
                    // New variable — allocate slot.
                    var newSlot = scope.AllocateLocal(varExpr.Name);
                    result.Add(PopSlot(newSlot, assign.SourceLine));
                }
            }
            else if (assign.Target is MemberAccessExpression2 memberAccess)
            {
                // obj.Field := value  OR  obj.Field = value
                // V1 setobj convention: push obj; push fieldName; push value; setobj; pop [resultSlot]
                EmitExpression(memberAccess.Object, scope, result);
                result.Add(PushString(memberAccess.MemberName, assign.SourceLine));
                EmitExpression(assign.Value, scope, result);
                result.Add(Emit(Opcodes.SetObj, assign.SourceLine));
                var setObjResultSlot = scope.AllocateTemp();
                result.Add(PopSlot(setObjResultSlot, assign.SourceLine));
            }
        }

        // ─── Compound assignment ──────────────────────────────────────────────────

        private void EmitCompoundAssignment(CompoundAssignmentStatement2 compound, ScopeSymbols2 scope, ExecutionBlock result)
        {
            // db1.Message<> += value  →
            //   push [db1]; push string: "Message<>"; getobj; pop [tmp]
            //   push [tmp]; push [value]; push 1; callobj "add"; pop [tmp]
            //   push [db1]; push string: "Message<>"; push [tmp]; setobj; pop [slot]
            if (compound.Target is MemberAccessExpression2 memberAccess)
            {
                var tmpSlot = scope.AllocateTemp();
                var resultSlot = scope.AllocateTemp();

                // Get current value: obj.Member
                EmitExpression(memberAccess.Object, scope, result);
                result.Add(PushString(memberAccess.MemberName, compound.SourceLine));
                result.Add(Emit(Opcodes.GetObj, compound.SourceLine));
                result.Add(PopSlot(tmpSlot, compound.SourceLine));

                // Call "add" on it with the RHS
                result.Add(PushSlot(tmpSlot, compound.SourceLine));
                EmitExpression(compound.Value, scope, result);
                result.Add(PushInt(1L, compound.SourceLine));
                result.Add(new Command { Opcode = Opcodes.CallObj, Operand1 = "add", SourceLine = compound.SourceLine });
                result.Add(PopSlot(tmpSlot, compound.SourceLine));

                // Store back: obj.Member = tmp
                EmitExpression(memberAccess.Object, scope, result);
                result.Add(PushString(memberAccess.MemberName, compound.SourceLine));
                result.Add(PushSlot(tmpSlot, compound.SourceLine));
                result.Add(Emit(Opcodes.SetObj, compound.SourceLine));
                result.Add(PopSlot(resultSlot, compound.SourceLine));
            }
        }

        // ─── streamwait call statement ────────────────────────────────────────────

        private void EmitStreamWaitCallStatement(StreamWaitCallStatement2 swCall, ScopeSymbols2 scope, ExecutionBlock result)
        {
            // streamwait print(message) →
            //   push string: "print"; push [message]; push 1; streamwait
            result.Add(PushString(swCall.FunctionName, swCall.SourceLine));
            EmitCallArguments(swCall.Arguments, scope, result, swCall.SourceLine);
            result.Add(Emit(Opcodes.StreamWait, swCall.SourceLine));
        }

        // ─── Call statement ───────────────────────────────────────────────────────

        private void EmitCallStatement(CallStatement2 call, ScopeSymbols2 scope, ExecutionBlock result)
        {
            if (call.Callee is VariableExpression2 varExpr)
            {
                if (string.Equals(varExpr.Name, "await", StringComparison.OrdinalIgnoreCase) && call.Arguments.Count == 1)
                {
                    // await obj — push obj, awaitobj, pop (discard result in statement context)
                    EmitExpression(call.Arguments[0], scope, result);
                    result.Add(Emit(Opcodes.AwaitObj, call.SourceLine));
                    result.Add(Emit(Opcodes.Pop, call.SourceLine));
                    return;
                }

                // Simple function/procedure call: Foo(a, b)
                EmitCallArguments(call.Arguments, scope, result, call.SourceLine);
                var callCmd = new Command
                {
                    Opcode = call.IsAsync ? Opcodes.ACall : Opcodes.Call,
                    Operand1 = new CallInfo { FunctionName = varExpr.Name },
                    SourceLine = call.SourceLine
                };
                result.Add(callCmd);
            }
            else if (call.Callee is MemberAccessExpression2 memberAccess)
            {
                // obj.Method(a, b) — as statement, result is discarded
                EmitExpression(memberAccess.Object, scope, result);
                EmitCallArguments(call.Arguments, scope, result, call.SourceLine);

                var callCmd = new Command
                {
                    Opcode = Opcodes.CallObj,
                    Operand1 = memberAccess.MemberName,
                    SourceLine = call.SourceLine
                };
                result.Add(callCmd);
                // V1 always emits pop after callobj statements (result discarded)
                result.Add(Emit(Opcodes.Pop, call.SourceLine));
            }
            else
            {
                // Generic expression call.
                EmitCallArguments(call.Arguments, scope, result, call.SourceLine);
                EmitExpression(call.Callee, scope, result);
            }
        }

        private void EmitCallArguments(List<ExpressionNode2> args, ScopeSymbols2 scope, ExecutionBlock result, int sourceLine)
        {
            // Push each argument first, then arity — matching V1 calling convention.
            foreach (var arg in args)
                EmitExpression(arg, scope, result);
            result.Add(PushInt(args.Count, sourceLine));
        }

        // ─── Return ───────────────────────────────────────────────────────────────

        private void EmitReturnStatement(ReturnStatement2 ret, ScopeSymbols2 scope, ExecutionBlock result)
        {
            if (ret.Value != null)
                EmitExpression(ret.Value, scope, result);
            result.Add(Emit(Opcodes.Ret, ret.SourceLine));
        }

        // ─── If statement ─────────────────────────────────────────────────────────

        private static int _labelCounter;

        private void EmitIfStatement(IfStatement2 ifStmt, ScopeSymbols2 scope, ExecutionBlock result, bool isProcedure)
        {
            // Use V1-compatible label names: if_end_N where N = ++counter + 1000
            var raw = System.Threading.Interlocked.Increment(ref _labelCounter);
            var ifId = raw + 1000;
            var elseLabel = $"if_else_{ifId}";
            var endLabel  = ifStmt.ElseBlock != null ? $"if_else_end_{ifId}" : $"if_end_{ifId}";
            // V1 uses "if_end_N" for simple if (no else), "if_else_N"/"if_else_end_N" for if/else.
            // For the golden reference we need: je if_end_N (no else), or je if_else_N; jmp if_else_end_N.
            var jumpTarget = ifStmt.ElseBlock != null ? elseLabel : endLabel;

            // Evaluate condition into a temp slot, then cmp [slot], 0; je label.
            var condSlot = scope.AllocateTemp();
            EmitExpression(ifStmt.Condition, scope, result);
            result.Add(PopSlot(condSlot, ifStmt.SourceLine));

            result.Add(new Command
            {
                Opcode = Opcodes.Cmp,
                Operand1 = new MemoryAddress { Index = condSlot },
                Operand2 = 0L,
                SourceLine = ifStmt.SourceLine
            });
            result.Add(new Command { Opcode = Opcodes.Je, Operand1 = jumpTarget, SourceLine = ifStmt.SourceLine });

            // Then block.
            var thenBlock = EmitBlock(ifStmt.ThenBlock, scope, isProcedure);
            result.AddRange(thenBlock);

            if (ifStmt.ElseBlock != null)
            {
                // Jump past else block.
                result.Add(new Command { Opcode = Opcodes.Jmp, Operand1 = endLabel, SourceLine = ifStmt.SourceLine });
                // Else label.
                result.Add(new Command { Opcode = Opcodes.Label, Operand1 = elseLabel, SourceLine = ifStmt.SourceLine });

                var elseBlock = EmitBlock(ifStmt.ElseBlock, scope, isProcedure);
                result.AddRange(elseBlock);
            }

            // End label.
            result.Add(new Command { Opcode = Opcodes.Label, Operand1 = endLabel, SourceLine = ifStmt.SourceLine });
        }

        // ─── Switch statement ─────────────────────────────────────────────────────

        private void EmitSwitchStatement(SwitchStatement2 switchStmt, ScopeSymbols2 scope, ExecutionBlock result, bool isProcedure)
        {
            var id = System.Threading.Interlocked.Increment(ref _labelCounter);
            var endLabel = $"__endswitch_{id}";

            // Evaluate the switch expression once into a temp slot.
            var tempSlot = scope.AllocateLocal($"__switch_{id}");
            EmitExpression(switchStmt.Expression, scope, result);
            result.Add(PopSlot(tempSlot, switchStmt.SourceLine));

            for (var i = 0; i < switchStmt.Cases.Count; i++)
            {
                var caseNode = switchStmt.Cases[i];
                var caseEnd = i + 1 < switchStmt.Cases.Count
                    ? $"__case_{id}_{i + 1}"
                    : (switchStmt.DefaultBlock != null ? $"__default_{id}" : endLabel);
                var caseStart = $"__case_{id}_{i}";

                // Label for this case.
                result.Add(new Command { Opcode = Opcodes.Label, Operand1 = caseStart, SourceLine = caseNode.SourceLine });

                // Compare switch value with case pattern.
                result.Add(PushSlot(tempSlot, caseNode.SourceLine));
                EmitExpression(caseNode.Pattern, scope, result);
                result.Add(Emit(Opcodes.Cmp, caseNode.SourceLine));
                result.Add(new Command { Opcode = Opcodes.Je, Operand1 = caseEnd, SourceLine = caseNode.SourceLine });

                // Case body.
                var caseBody = EmitBlock(caseNode.Body, scope, isProcedure);
                result.AddRange(caseBody);
                result.Add(new Command { Opcode = Opcodes.Jmp, Operand1 = endLabel, SourceLine = caseNode.SourceLine });
            }

            // Default block.
            if (switchStmt.DefaultBlock != null)
            {
                result.Add(new Command { Opcode = Opcodes.Label, Operand1 = $"__default_{id}", SourceLine = switchStmt.SourceLine });
                var defaultBody = EmitBlock(switchStmt.DefaultBlock, scope, isProcedure);
                result.AddRange(defaultBody);
            }

            result.Add(new Command { Opcode = Opcodes.Label, Operand1 = endLabel, SourceLine = switchStmt.SourceLine });
        }

        // ─── Stream wait by delta loop ────────────────────────────────────────────

        private static int _streamLoopCounter;

        /// <summary>
        /// Emits the V1-compatible inline label-based pattern for:
        ///   for streamwait by delta (streamExpr, deltaVar [, aggregateVar]) { body }
        ///
        /// Output structure (all inline, no separate procedure):
        ///   label streamwait_loop_N
        ///   push [streamSlot]; push string: "delta"; streamwaitobj; pop [endSlot]
        ///   cmp [endSlot], 1; je streamwait_loop_N_end
        ///   push [captured...]; push arity; acall streamwait_loop_N_delta
        ///   jmp streamwait_loop_N
        ///   label streamwait_loop_N_delta
        ///   [body instructions]
        ///   ret
        ///   label streamwait_loop_N_end
        ///   pop; pop; ret
        /// </summary>
        private void EmitStreamWaitByDeltaLoop(StreamWaitByDeltaLoop2 loop, ScopeSymbols2 scope, ExecutionBlock result, bool isProcedure)
        {
            var n = System.Threading.Interlocked.Increment(ref _streamLoopCounter);
            var loopLabel = $"streamwait_loop_{n}";
            var bodyLabel = $"streamwait_loop_{n}_delta";
            var endLabel  = $"streamwait_loop_{n}_end";

            // Allocate slots in V1 order: endSlot, deltaSlot, aggregateSlot
            var endSlot       = scope.AllocateTemp();
            var deltaSlot     = scope.AllocateLocal(loop.DeltaVarName);
            var aggregateSlot = string.IsNullOrEmpty(loop.AggregateVarName)
                ? scope.AllocateTemp()
                : scope.AllocateLocal(loop.AggregateVarName);

            // Track the first slot index of body locals (everything allocated before body is a parent slot).
            var bodySlotStart = scope.NextSlot;

            // ── Loop header ──────────────────────────────────────────────────────
            result.Add(new Command { Opcode = Opcodes.Label, Operand1 = loopLabel, SourceLine = loop.SourceLine });
            EmitExpression(loop.Stream, scope, result);
            result.Add(PushString(loop.WaitType, loop.SourceLine));
            result.Add(Emit(Opcodes.StreamWaitObj, loop.SourceLine));
            result.Add(PopSlot(endSlot, loop.SourceLine));
            result.Add(new Command
            {
                Opcode = Opcodes.Cmp,
                Operand1 = new MemoryAddress { Index = endSlot },
                Operand2 = 1L,
                SourceLine = loop.SourceLine
            });
            result.Add(new Command { Opcode = Opcodes.Je, Operand1 = endLabel, SourceLine = loop.SourceLine });

            // ── Compile body first to discover captured parent slots ─────────────
            var bodyBlock = EmitBlock(loop.Body, scope, isProcedure);

            // Determine which parent slots are referenced in body (slots < bodySlotStart, excluding delta/aggregate)
            var captureSlots = CollectCaptureSlots(bodyBlock, bodySlotStart, deltaSlot, aggregateSlot);

            // ── acall: push captured, push arity, acall bodyLabel ────────────────
            foreach (var capSlot in captureSlots)
                result.Add(PushSlot(capSlot, loop.SourceLine));
            result.Add(PushInt(2L + captureSlots.Count, loop.SourceLine));
            result.Add(new Command
            {
                Opcode = Opcodes.ACall,
                Operand1 = new CallInfo { FunctionName = bodyLabel },
                SourceLine = loop.SourceLine
            });
            result.Add(new Command { Opcode = Opcodes.Jmp, Operand1 = loopLabel, SourceLine = loop.SourceLine });

            // ── Body ─────────────────────────────────────────────────────────────
            result.Add(new Command { Opcode = Opcodes.Label, Operand1 = bodyLabel, SourceLine = loop.SourceLine });
            result.AddRange(bodyBlock);
            result.Add(Emit(Opcodes.Ret, loop.SourceLine));

            // ── End ──────────────────────────────────────────────────────────────
            result.Add(new Command { Opcode = Opcodes.Label, Operand1 = endLabel, SourceLine = loop.SourceLine });
            result.Add(Emit(Opcodes.Pop, loop.SourceLine));
            result.Add(Emit(Opcodes.Pop, loop.SourceLine));
        }

        /// <summary>
        /// Collect all local memory slot indices referenced in <paramref name="block"/>
        /// that are &lt; <paramref name="bodySlotStart"/> (parent scope), excluding the delta and aggregate slots.
        /// These are the slots that need to be captured (pushed before acall).
        /// </summary>
        private static List<int> CollectCaptureSlots(ExecutionBlock block, int bodySlotStart, int deltaSlot, int aggregateSlot)
        {
            var set = new System.Collections.Generic.SortedSet<int>();
            foreach (var cmd in block)
            {
                if (cmd.Opcode == Opcodes.Push && cmd.Operand1 is PushOperand po && po.Kind == "Memory")
                {
                    var idx = (int)(long)po.Value!;
                    if (idx < bodySlotStart && idx != deltaSlot && idx != aggregateSlot)
                        set.Add(idx);
                }
                if (cmd.Opcode == Opcodes.Cmp && cmd.Operand1 is MemoryAddress ma)
                {
                    // Cmp might also reference slots — but usually body-allocated
                }
            }
            return new List<int>(set);
        }

        // ─── Stream wait for loop ─────────────────────────────────────────────────

        private void EmitStreamWaitForLoop(StreamWaitForLoop2 forLoop, ScopeSymbols2 scope, ExecutionBlock result, bool isProcedure)
        {
            var id = System.Threading.Interlocked.Increment(ref _labelCounter);
            var loopLabel = $"__streamfor_{id}";
            var endLabel = $"__endstreamfor_{id}";

            var itemSlot = scope.AllocateLocal(forLoop.VariableName);

            // Evaluate the stream expression.
            EmitExpression(forLoop.Stream, scope, result);

            // Stream wait loop — emit streamwait/streamwaitobj.
            result.Add(new Command
            {
                Opcode = Opcodes.StreamWait,
                Operand1 = loopLabel,
                SourceLine = forLoop.SourceLine
            });

            result.Add(new Command { Opcode = Opcodes.Label, Operand1 = loopLabel, SourceLine = forLoop.SourceLine });
            result.Add(PopSlot(itemSlot, forLoop.SourceLine));

            var bodyBlock = EmitBlock(forLoop.Body, scope, isProcedure);
            result.AddRange(bodyBlock);

            result.Add(new Command { Opcode = Opcodes.Label, Operand1 = endLabel, SourceLine = forLoop.SourceLine });
        }

        // ─── Inline instruction statement ────────────────────────────────────────

        private void EmitInstructionStatement(InstructionStatement2 instr, ScopeSymbols2 scope, ExecutionBlock result)
        {
            var opcode = MapOpcode(instr.Opcode);
            var cmd = new Command { Opcode = opcode, SourceLine = instr.SourceLine };

            // For simple opcodes, map parameters directly.
            switch (opcode)
            {
                case Opcodes.Call:
                case Opcodes.ACall:
                {
                    var fnName = instr.Parameters.FirstOrDefault(p => p.ValueType == "function")?.Value as string ?? "";
                    cmd.Operand1 = new CallInfo { FunctionName = fnName };
                    break;
                }
                case Opcodes.Push:
                {
                    var param = instr.Parameters.FirstOrDefault();
                    if (param != null)
                    {
                        cmd.Operand1 = param.ValueType switch
                        {
                            "string" => new PushOperand { Kind = "StringLiteral", Value = param.Value?.ToString() },
                            "index" or "int" => new PushOperand { Kind = "IntLiteral", Value = param.Value },
                            "memory" => new PushOperand { Kind = "Memory", Value = param.Value },
                            _ => new PushOperand { Kind = "StringLiteral", Value = param.Value?.ToString() }
                        };
                    }
                    break;
                }
                case Opcodes.Pop:
                {
                    var param = instr.Parameters.FirstOrDefault();
                    if (param?.Value is int slotIdx)
                        cmd.Operand1 = new MemoryAddress { Index = slotIdx };
                    break;
                }
                default:
                    // For other opcodes use the raw param list as Operand1.
                    if (instr.Parameters.Count > 0)
                        cmd.Operand1 = instr.Parameters.FirstOrDefault()?.Value;
                    break;
            }

            result.Add(cmd);
        }

        // ─── Expression emission ──────────────────────────────────────────────────

        /// <summary>
        /// Emit bytecode for an expression node by directly walking the typed AST.
        /// This is the v2.0 replacement for the text-parsing in v1.0's expression lowering.
        /// </summary>
        public void EmitExpression(ExpressionNode2 expr, ScopeSymbols2 scope, ExecutionBlock result)
        {
            switch (expr)
            {
                case LiteralExpression2 lit:
                    EmitLiteral(lit, result);
                    break;

                case VariableExpression2 varExpr:
                    EmitVariableRef(varExpr, scope, result);
                    break;

                case MemberAccessExpression2 memberAccess:
                    EmitMemberAccess(memberAccess, scope, result);
                    break;

                case CallExpression2 call:
                    EmitCallExpression(call, scope, result);
                    break;

                case BinaryExpression2 binary:
                    EmitBinaryExpression(binary, scope, result);
                    break;

                case UnaryExpression2 unary:
                    EmitUnaryExpression(unary, scope, result);
                    break;

                case AwaitExpression2 awaitExpr:
                    EmitAwaitExpression(awaitExpr, scope, result);
                    break;

                case ObjectCreationExpression2 objCreate:
                    EmitObjectCreation(objCreate, scope, result);
                    break;

                case GenericTypeExpression2 generic:
                    EmitGenericType(generic, scope, result);
                    break;

                case MemorySlotExpression2 slot:
                    result.Add(PushSlot(slot.SlotIndex, slot.SourceLine));
                    break;

                case LambdaExpression2 lambda:
                    EmitLambda(lambda, scope, result);
                    break;

                case ObjectLiteralExpression2 objLit:
                    EmitObjectLiteral(objLit, scope, result);
                    break;

                case SymbolicExpression2 symbolic:
                    EmitSymbolicExpression(symbolic, result);
                    break;

                case NullAssertExpression2 nullAssert:
                    // Null-assertion is transparent — just emit the inner operand.
                    EmitExpression(nullAssert.Operand, scope, result);
                    break;

                default:
                    result.Add(Emit(Opcodes.Nop, expr.SourceLine));
                    break;
            }
        }

        private static void EmitLiteral(LiteralExpression2 lit, ExecutionBlock result)
        {
            result.Add(lit.Kind switch
            {
                LiteralKind2.String => new Command
                {
                    Opcode = Opcodes.Push,
                    Operand1 = new PushOperand { Kind = "StringLiteral", Value = lit.Value?.ToString() ?? "" },
                    SourceLine = lit.SourceLine
                },
                LiteralKind2.Integer => new Command
                {
                    Opcode = Opcodes.Push,
                    Operand1 = new PushOperand { Kind = "IntLiteral", Value = lit.Value },
                    SourceLine = lit.SourceLine
                },
                LiteralKind2.Float => new Command
                {
                    Opcode = Opcodes.Push,
                    Operand1 = new PushOperand { Kind = "FloatLiteral", Value = lit.Value },
                    SourceLine = lit.SourceLine
                },
                LiteralKind2.Boolean => new Command
                {
                    Opcode = Opcodes.Push,
                    Operand1 = new PushOperand { Kind = "IntLiteral", Value = (bool)(lit.Value ?? false) ? 1L : 0L },
                    SourceLine = lit.SourceLine
                },
                _ => new Command
                {
                    Opcode = Opcodes.Push,
                    Operand1 = new PushOperand { Kind = "IntLiteral", Value = 0L },
                    SourceLine = lit.SourceLine
                }
            });
        }

        private static void EmitVariableRef(VariableExpression2 varExpr, ScopeSymbols2 scope, ExecutionBlock result)
        {
            if (scope.TryResolveSlot(varExpr.Name, out var slot, out var kind))
            {
                result.Add(new Command
                {
                    Opcode = Opcodes.Push,
                    Operand1 = new PushOperand
                    {
                        Kind = kind == "global" ? "Global" : "Memory",
                        Value = (long)slot
                    },
                    SourceLine = varExpr.SourceLine
                });
            }
            else
            {
                // Unresolved — bare identifier (type name like vault, stream, database, etc.)
                result.Add(PushIdentifier(varExpr.Name, varExpr.SourceLine));
            }
        }

        private void EmitMemberAccess(MemberAccessExpression2 memberAccess, ScopeSymbols2 scope, ExecutionBlock result)
        {
            EmitExpression(memberAccess.Object, scope, result);
            result.Add(new Command
            {
                Opcode = Opcodes.Push,
                Operand1 = new PushOperand { Kind = "StringLiteral", Value = memberAccess.MemberName },
                SourceLine = memberAccess.SourceLine
            });
            result.Add(Emit(Opcodes.GetObj, memberAccess.SourceLine));
        }

        private void EmitCallExpression(CallExpression2 call, ScopeSymbols2 scope, ExecutionBlock result)
        {
            if (call.Callee is MemberAccessExpression2 memberAccess)
            {
                // obj.Method(args) — emit as callobj.
                EmitExpression(memberAccess.Object, scope, result);
                EmitCallArguments(call.Arguments, scope, result, call.SourceLine);

                result.Add(new Command
                {
                    Opcode = Opcodes.CallObj,
                    Operand1 = memberAccess.MemberName,
                    SourceLine = call.SourceLine
                });
            }
            else if (call.Callee is VariableExpression2 varExpr)
            {
                // Regular function call: push args then call.
                EmitCallArguments(call.Arguments, scope, result, call.SourceLine);
                result.Add(new Command
                {
                    Opcode = call.IsAsync ? Opcodes.ACall : Opcodes.Call,
                    Operand1 = new CallInfo { FunctionName = varExpr.Name },
                    SourceLine = call.SourceLine
                });
            }
        }

        private void EmitBinaryExpression(BinaryExpression2 binary, ScopeSymbols2 scope, ExecutionBlock result)
        {
            EmitExpression(binary.Left, scope, result);
            EmitExpression(binary.Right, scope, result);

            var opCode = binary.Operator switch
            {
                "+" => Opcodes.Add,
                "-" => Opcodes.Sub,
                "*" => Opcodes.Mul,
                "/" => Opcodes.Div,
                "==" => Opcodes.Equals,
                "!=" => Opcodes.Equals, // followed by Not
                "<" => Opcodes.Lt,
                _ => Opcodes.Cmp
            };

            result.Add(Emit(opCode, binary.SourceLine));

            if (binary.Operator == "!=")
                result.Add(Emit(Opcodes.Not, binary.SourceLine));
        }

        private void EmitUnaryExpression(UnaryExpression2 unary, ScopeSymbols2 scope, ExecutionBlock result)
        {
            EmitExpression(unary.Operand, scope, result);
            if (unary.Operator == "!")
                result.Add(Emit(Opcodes.Not, unary.SourceLine));
        }

        private void EmitAwaitExpression(AwaitExpression2 awaitExpr, ScopeSymbols2 scope, ExecutionBlock result)
        {
            EmitExpression(awaitExpr.Operand, scope, result);
            result.Add(Emit(awaitExpr.IsObjectAwait ? Opcodes.AwaitObj : Opcodes.Await, awaitExpr.SourceLine));
        }

        private void EmitObjectCreation(ObjectCreationExpression2 objCreate, ScopeSymbols2 scope, ExecutionBlock result)
        {
            result.Add(PushString(objCreate.TypeName, objCreate.SourceLine));

            if (objCreate.PositionalArgs.Count > 0)
            {
                result.Add(PushInt(objCreate.PositionalArgs.Count, objCreate.SourceLine));
                foreach (var arg in objCreate.PositionalArgs)
                    EmitExpression(arg, scope, result);

                result.Add(new Command
                {
                    Opcode = Opcodes.Call,
                    Operand1 = new CallInfo
                    {
                        FunctionName = $"{objCreate.TypeName}_ctor_{objCreate.PositionalArgs.Count}"
                    },
                    SourceLine = objCreate.SourceLine
                });
            }
            else
            {
                result.Add(Emit(Opcodes.Def, objCreate.SourceLine));
            }
        }

        private static void EmitGenericType(GenericTypeExpression2 generic, ScopeSymbols2 scope, ExecutionBlock result)
        {
            // V1 pattern for stream<A, B>:
            //   push typeName   ← bare identifier (not string literal)
            //   push 1          ← arity for def
            //   def             ← create base type instance
            //   pop [tempSlot]  ← store base instance
            //   push [tempSlot] ← push base instance for defgen
            //   push A          ← type arg(s) as bare identifiers
            //   push B
            //   push N          ← arg count for defgen
            //   defgen          ← specialize
            // (caller stores result via PopSlot)
            var tempSlot = scope.AllocateTemp();

            // Push type name as bare identifier (push stream, not push string: "stream")
            result.Add(PushIdentifier(generic.TypeName, generic.SourceLine));
            result.Add(PushInt(1L, generic.SourceLine));
            result.Add(new Command { Opcode = Opcodes.Def, SourceLine = generic.SourceLine });
            result.Add(PopSlot(tempSlot, generic.SourceLine));
            result.Add(PushSlot(tempSlot, generic.SourceLine));

            // Push each type argument as bare identifier or global slot reference.
            var typeArgs = generic.TypeArg.Split(',');
            foreach (var arg in typeArgs)
            {
                var a = arg.Trim();
                if (string.IsNullOrEmpty(a)) continue;

                // Check if this type arg is a known global type (e.g. Db> → global: [1])
                if (scope.TryResolveSlot(a, out var argSlot, out var argKind))
                {
                    // It's a known variable/type — push as global or memory slot
                    result.Add(new Command
                    {
                        Opcode = Opcodes.Push,
                        Operand1 = new PushOperand { Kind = argKind == "global" ? "Global" : "Memory", Value = (long)argSlot },
                        SourceLine = generic.SourceLine
                    });
                }
                else
                {
                    // Unknown identifier — push as bare identifier (not string literal)
                    result.Add(PushIdentifier(a, generic.SourceLine));
                }
            }

            var argCount = typeArgs.Count(a => !string.IsNullOrWhiteSpace(a));
            result.Add(PushInt((long)argCount, generic.SourceLine));
            result.Add(new Command { Opcode = Opcodes.DefGen, SourceLine = generic.SourceLine });
        }

        private void EmitLambda(LambdaExpression2 lambda, ScopeSymbols2 scope, ExecutionBlock result)
        {
            // Lambda: emit Lambda opcode followed by body, then DefExpr.
            result.Add(new Command { Opcode = Opcodes.Lambda, SourceLine = lambda.SourceLine });
            var lambdaBody = EmitBlock(lambda.Body, scope, isProcedure: false);
            result.AddRange(lambdaBody);
            result.Add(Emit(Opcodes.DefExpr, lambda.SourceLine));
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private static Command Emit(Opcodes opcode, int sourceLine) =>
            new() { Opcode = opcode, SourceLine = sourceLine };

        private static Command PushString(string value, int sourceLine) => new()
        {
            Opcode = Opcodes.Push,
            Operand1 = new PushOperand { Kind = "StringLiteral", Value = value },
            SourceLine = sourceLine
        };

        private static Command PushInt(long value, int sourceLine) => new()
        {
            Opcode = Opcodes.Push,
            Operand1 = new PushOperand { Kind = "IntLiteral", Value = value },
            SourceLine = sourceLine
        };

        private static Command PushSlot(int slot, int sourceLine) => new()
        {
            Opcode = Opcodes.Push,
            Operand1 = new PushOperand { Kind = "Memory", Value = (long)slot },
            SourceLine = sourceLine
        };

        private static Command PopSlot(int slot, int sourceLine) => new()
        {
            Opcode = Opcodes.Pop,
            Operand1 = new MemoryAddress { Index = slot },
            SourceLine = sourceLine
        };

        /// <summary>
        /// Push a bare identifier (type name, keyword) — serializes as "push stream" not "push string: "stream"".
        /// In the Command model, this is a Push with Kind="StringLiteral" but the AGIASM printer
        /// uses "Identifier" kind for bare names.  We use a dedicated kind "Identifier" here.
        /// </summary>
        private static Command PushIdentifier(string name, int sourceLine) => new()
        {
            Opcode = Opcodes.Push,
            Operand1 = new PushOperand { Kind = "Identifier", Value = name },
            SourceLine = sourceLine
        };

        // ─── Object literal emission ──────────────────────────────────────────────

        /// <summary>
        /// Emit an object literal { key1: val1, key2: val2 } as:
        ///   push string: "{}"; pop [slot];
        ///   call opjson, [slot], operation: "set", path: "key1", [val1slot]; pop
        ///   call opjson, [slot], operation: "set", path: "key2", [val2slot]; pop
        ///   push [slot]  ← leaves result on stack for caller
        ///
        /// For simple variable expressions that resolve to a known slot, the slot is used directly
        /// (no extra temp). For complex expressions, a temp slot is allocated.
        /// </summary>
        private void EmitObjectLiteral(ObjectLiteralExpression2 objLit, ScopeSymbols2 scope, ExecutionBlock result)
        {
            var objSlot = scope.AllocateTemp();
            result.Add(PushString("{}", objLit.SourceLine));
            result.Add(PopSlot(objSlot, objLit.SourceLine));

            foreach (var (key, valExpr) in objLit.Properties)
            {
                // Resolve value expression to a slot without emitting extra instructions for simple var refs.
                int valSlot;
                if (valExpr is VariableExpression2 ve && scope.TryResolveSlot(ve.Name, out var resolvedSlot, out _))
                {
                    // Direct slot reference — no temp needed.
                    valSlot = resolvedSlot;
                }
                else if (valExpr is MemberAccessExpression2 ma)
                {
                    // Complex expression — emit into temp.
                    valSlot = scope.AllocateTemp();
                    EmitExpression(valExpr, scope, result);
                    result.Add(PopSlot(valSlot, objLit.SourceLine));
                }
                else
                {
                    // Complex expression — emit into temp.
                    valSlot = scope.AllocateTemp();
                    EmitExpression(valExpr, scope, result);
                    result.Add(PopSlot(valSlot, objLit.SourceLine));
                }
                EmitOpJsonSet(objSlot, key, valSlot, objLit.SourceLine, result);
            }

            result.Add(PushSlot(objSlot, objLit.SourceLine));
        }

        /// <summary>Emit: call opjson, [objSlot], operation: "set", path: "key", [valSlot]; pop</summary>
        private static void EmitOpJsonSet(int objSlot, string key, int valSlot, int sourceLine, ExecutionBlock result)
        {
            result.Add(new Command
            {
                Opcode = Opcodes.Call,
                Operand1 = new CallInfo
                {
                    FunctionName = "opjson",
                    Parameters = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "source", new MemoryAddress { Index = objSlot } },
                        { "operation", "set" },
                        { "path", key },
                        { "data", new MemoryAddress { Index = valSlot } }
                    }
                },
                SourceLine = sourceLine
            });
            result.Add(Emit(Opcodes.Pop, sourceLine));
        }

        // ─── Symbolic expression emission ─────────────────────────────────────────

        /// <summary>
        /// Emit: push string: ":name"; push 1; call get
        /// </summary>
        private static void EmitSymbolicExpression(SymbolicExpression2 symbolic, ExecutionBlock result)
        {
            result.Add(PushString(":" + symbolic.Name, symbolic.SourceLine));
            result.Add(PushInt(1L, symbolic.SourceLine));
            result.Add(new Command
            {
                Opcode = Opcodes.Call,
                Operand1 = new CallInfo { FunctionName = "get" },
                SourceLine = symbolic.SourceLine
            });
        }

        private static Opcodes MapOpcode(string opcode)
        {
            return opcode.ToLowerInvariant() switch
            {
                "addvertex" => Opcodes.AddVertex,
                "addrelation" => Opcodes.AddRelation,
                "addshape" => Opcodes.AddShape,
                "call" => Opcodes.Call,
                "acall" => Opcodes.ACall,
                "callobj" => Opcodes.CallObj,
                "push" => Opcodes.Push,
                "pop" => Opcodes.Pop,
                "def" => Opcodes.Def,
                "defgen" => Opcodes.DefGen,
                "defobj" => Opcodes.DefObj,
                "defexpr" => Opcodes.DefExpr,
                "await" => Opcodes.Await,
                "awaitobj" => Opcodes.AwaitObj,
                "streamwait" => Opcodes.StreamWait,
                "streamwaitobj" => Opcodes.StreamWaitObj,
                "lambda" => Opcodes.Lambda,
                "expr" => Opcodes.Expr,
                "label" => Opcodes.Label,
                "je" => Opcodes.Je,
                "jmp" => Opcodes.Jmp,
                "cmp" => Opcodes.Cmp,
                "equals" => Opcodes.Equals,
                "not" => Opcodes.Not,
                "lt" => Opcodes.Lt,
                "add" => Opcodes.Add,
                "sub" => Opcodes.Sub,
                "mul" => Opcodes.Mul,
                "div" => Opcodes.Div,
                "pow" => Opcodes.Pow,
                "ret" => Opcodes.Ret,
                "getobj" => Opcodes.GetObj,
                "setobj" => Opcodes.SetObj,
                "getvertex" => Opcodes.GetVertex,
                "syscall" => Opcodes.SysCall,
                "nop" => Opcodes.Nop,
                _ => Opcodes.Nop
            };
        }
    }

    /// <summary>
    /// Private scope implementation used by <see cref="Assembler2"/> for procedure/function assembly.
    /// Inherits the global slot counter from the symbol table so procedure slots continue after global ones.
    /// </summary>
    internal sealed class ScopeSymbols2Private : ScopeSymbols2
    {
        public ScopeSymbols2Private(SymbolTable2 table)
            : base(table, new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase), new Dictionary<string, TypeDeclarationNode2>(StringComparer.OrdinalIgnoreCase), isGlobal: false)
        {
        }
    }
}
