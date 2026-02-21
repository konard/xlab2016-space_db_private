using Magic.Kernel.Compilation.Ast;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Magic.Kernel.Compilation
{
    /// <summary>
    /// Lowers high-level statement lines into instruction AST nodes.
    /// </summary>
    internal sealed class StatementLoweringCompiler
    {
        private Scanner? _scanner;

        private Scanner CurrentScanner =>
            _scanner ?? throw new InvalidOperationException("Scanner is not initialized.");

        public List<InstructionNode> Lower(IEnumerable<string> sourceLines)
        {
            return CompileStatementLines(sourceLines);
        }

        private List<InstructionNode> CompileStatementLines(IEnumerable<string> sourceLines)
        {
            var instructions = new List<InstructionNode>();
            var vars = new Dictionary<string, (string Kind, int Index)>(StringComparer.Ordinal);
            var vertexCounter = 1;
            var relationCounter = 1;
            var shapeCounter = 1;
            var memorySlotCounter = 0;
            var inVarBlock = false;
            var varBlockLines = new List<string>();

            foreach (var rawLine in sourceLines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line == "{" || line == "}")
                    continue;

                if (TryParseVarKeywordOnly(line))
                {
                    inVarBlock = true;
                    varBlockLines.Clear();
                    continue;
                }

                if (TryStripInlineVarPrefix(line, out var inlineDecl) && IsDeclarationStatement(inlineDecl))
                {
                    inVarBlock = true;
                    varBlockLines.Clear();
                    varBlockLines.Add(inlineDecl);
                    continue;
                }

                if (inVarBlock)
                {
                    if (IsDeclarationStatement(line))
                    {
                        varBlockLines.Add(line);
                        continue;
                    }

                    inVarBlock = false;
                    instructions.AddRange(CompileVarBlock(varBlockLines, vars, ref vertexCounter, ref relationCounter, ref shapeCounter, ref memorySlotCounter));
                }

                if (IsDeclarationStatement(line))
                {
                    instructions.AddRange(CompileVarBlock(new List<string> { line }, vars, ref vertexCounter, ref relationCounter, ref shapeCounter, ref memorySlotCounter));
                    continue;
                }

                if (TryCompileMethodCall(line, vars, instructions))
                    continue;

                if (TryCompileAssignment(line, vars, ref shapeCounter, ref vertexCounter, ref memorySlotCounter, instructions))
                    continue;

                if (TryCompileFunctionCall(line, vars, instructions))
                    continue;

                if (TryParseProcedureName(line, out var procName))
                    instructions.Add(CreateCallInstruction(procName));
            }

            if (inVarBlock && varBlockLines.Count > 0)
                instructions.AddRange(CompileVarBlock(varBlockLines, vars, ref vertexCounter, ref relationCounter, ref shapeCounter, ref memorySlotCounter));

            return instructions;
        }

        private sealed class VertexInitSpec
        {
            public List<long> Dimensions { get; } = new List<long> { 1, 0, 0, 0 };
            public double Weight { get; set; } = 0.5d;
            public string? TextData { get; set; }
            public string? BinaryData { get; set; }
        }

        private sealed class RelationInitSpec
        {
            public string? From { get; set; }
            public string? To { get; set; }
            public double Weight { get; set; } = 0.5d;
        }

        private sealed class InlineVertexSpec
        {
            public List<long> Dimensions { get; } = new List<long> { 1, 0, 0, 0 };
            public double? Weight { get; set; }
        }

        private sealed class ShapeInitSpec
        {
            public List<string> VertexNames { get; } = new List<string>();
            public List<InlineVertexSpec> InlineVertices { get; } = new List<InlineVertexSpec>();
            public bool UsesInlineVertices => InlineVertices.Count > 0;
        }

        private List<InstructionNode> CompileVarBlock(List<string> varLines, Dictionary<string, (string Kind, int Index)> vars, ref int vertexCounter, ref int relationCounter, ref int shapeCounter, ref int memorySlotCounter)
        {
            var instructions = new List<InstructionNode>();

            foreach (var line in varLines)
            {
                if (TryParseStreamDeclaration(line, out var streamName, out var elementType))
                {
                    EmitStreamDeclaration(streamName, elementType, vars, ref memorySlotCounter, instructions);
                    continue;
                }

                if (!TryParseEntityDeclaration(line, out var varName, out var varType, out var initText))
                    continue;

                if (varType == "vertex")
                {
                    var index = vertexCounter++;
                    vars[varName] = ("vertex", index);
                    instructions.AddRange(CompileVertexInit(index, initText));
                }
                else if (varType == "relation")
                {
                    var index = relationCounter++;
                    vars[varName] = ("relation", index);
                    instructions.AddRange(CompileRelationInit(index, initText, vars));
                }
                else if (varType == "shape")
                {
                    var index = shapeCounter++;
                    vars[varName] = ("shape", index);
                    instructions.AddRange(CompileShapeInit(index, initText, vars, ref vertexCounter));
                }
            }

            return instructions;
        }

        private List<InstructionNode> CompileVertexInit(int index, string initValue)
        {
            var instructions = new List<InstructionNode>();
            if (!TryParseVertexObject(initValue, out var spec))
                return instructions;

            var parameters = new List<ParameterNode>
            {
                new IndexParameterNode { Name = "index", Value = index },
                new DimensionsParameterNode { Name = "dimensions", Values = spec.Dimensions.Select(v => (float)v).ToList() },
                new WeightParameterNode { Name = "weight", Value = (float)spec.Weight }
            };

            if (!string.IsNullOrEmpty(spec.BinaryData))
            {
                parameters.Add(new DataParameterNode
                {
                    Name = "data",
                    Type = "binary:base64",
                    Types = new List<string> { "binary", "base64" },
                    Value = spec.BinaryData,
                    HasColon = true
                });
            }
            else if (!string.IsNullOrEmpty(spec.TextData))
            {
                parameters.Add(new DataParameterNode
                {
                    Name = "data",
                    Type = "text",
                    Types = new List<string> { "text" },
                    Value = spec.TextData,
                    HasColon = true
                });
            }

            instructions.Add(new InstructionNode { Opcode = "addvertex", Parameters = parameters });
            return instructions;
        }

        private List<InstructionNode> CompileRelationInit(int index, string initValue, Dictionary<string, (string Kind, int Index)> vars)
        {
            var instructions = new List<InstructionNode>();
            if (!TryParseRelationObject(initValue, out var spec) || string.IsNullOrEmpty(spec.From) || string.IsNullOrEmpty(spec.To))
                return instructions;

            if (!vars.TryGetValue(spec.From, out var fromVar) || !vars.TryGetValue(spec.To, out var toVar))
                return instructions;

            var fromType = fromVar.Kind == "relation" ? "relation" : "vertex";
            var toType = toVar.Kind == "relation" ? "relation" : "vertex";

            instructions.Add(new InstructionNode
            {
                Opcode = "addrelation",
                Parameters = new List<ParameterNode>
                {
                    new IndexParameterNode { Name = "index", Value = index },
                    new FromParameterNode { Name = "from", EntityType = fromType, Index = fromVar.Index },
                    new ToParameterNode { Name = "to", EntityType = toType, Index = toVar.Index },
                    new WeightParameterNode { Name = "weight", Value = (float)spec.Weight }
                }
            });
            return instructions;
        }

        private List<InstructionNode> CompileShapeInit(int index, string initValue, Dictionary<string, (string Kind, int Index)> vars, ref int vertexCounter)
        {
            var instructions = new List<InstructionNode>();
            if (!TryParseShapeObject(initValue, out var spec))
                return instructions;

            var indices = new List<long>();
            if (spec.UsesInlineVertices)
            {
                foreach (var vertex in spec.InlineVertices)
                {
                    var tempIndex = vertexCounter++;
                    var parameters = new List<ParameterNode>
                    {
                        new IndexParameterNode { Name = "index", Value = tempIndex },
                        new DimensionsParameterNode { Name = "dimensions", Values = vertex.Dimensions.Select(v => (float)v).ToList() }
                    };
                    if (vertex.Weight.HasValue)
                        parameters.Add(new WeightParameterNode { Name = "weight", Value = (float)vertex.Weight.Value });
                    instructions.Add(new InstructionNode { Opcode = "addvertex", Parameters = parameters });
                    indices.Add(tempIndex);
                }
            }
            else
            {
                foreach (var name in spec.VertexNames)
                {
                    if (vars.TryGetValue(name, out var vertexVar) && vertexVar.Kind == "vertex")
                        indices.Add(vertexVar.Index);
                }
            }

            if (indices.Count > 0)
            {
                instructions.Add(new InstructionNode
                {
                    Opcode = "addshape",
                    Parameters = new List<ParameterNode>
                    {
                        new IndexParameterNode { Name = "index", Value = index },
                        new VerticesParameterNode { Name = "vertices", Indices = indices }
                    }
                });
            }

            return instructions;
        }

        private bool TryCompileAssignment(string line, Dictionary<string, (string Kind, int Index)> vars, ref int shapeCounter, ref int vertexCounter, ref int memorySlotCounter, List<InstructionNode> instructions)
        {
            var previous = _scanner;
            _scanner = new Scanner(line);
            try
            {
                if (IsIdentifier(CurrentScanner.Current, "var"))
                {
                    CurrentScanner.Scan();
                }

                if (CurrentScanner.Current.Kind != TokenKind.Identifier)
                    return false;
                var targetName = CurrentScanner.Scan().Value;

                var hasAssign = CurrentScanner.Current.Kind == TokenKind.Assign;
                var hasDefineAssign = CurrentScanner.Current.Kind == TokenKind.Colon && CurrentScanner.Watch(1)?.Kind == TokenKind.Assign;
                if (!hasAssign && !hasDefineAssign)
                    return false;
                if (hasDefineAssign)
                {
                    CurrentScanner.Scan();
                    CurrentScanner.Scan();
                }
                else
                {
                    CurrentScanner.Scan();
                }

                SkipSemicolon();
                if (CurrentScanner.Current.IsEndOfInput)
                    return true;

                if (IsIdentifier(CurrentScanner.Current, "vault"))
                {
                    var vaultSlot = memorySlotCounter++;
                    vars[targetName] = ("vault", vaultSlot);
                    return true;
                }

                if (TryParseVaultReadExpression(out var vaultVarName, out var tokenKey) &&
                    vars.TryGetValue(vaultVarName, out var vaultVar) &&
                    vaultVar.Kind == "vault")
                {
                    var resultSlot = memorySlotCounter++;
                    vars[targetName] = ("memory", resultSlot);
                    instructions.Add(CreatePushStringInstruction(tokenKey));
                    instructions.Add(CreatePushIntInstruction(1));
                    instructions.Add(CreateCallInstruction("vault_read"));
                    instructions.Add(CreatePopMemoryInstruction(resultSlot));
                    return true;
                }

                if (TryParseAwaitExpression(out var awaitVarName) &&
                    vars.TryGetValue(awaitVarName, out var streamVar) &&
                    streamVar.Kind == "stream")
                {
                    var dataSlot = memorySlotCounter++;
                    vars[targetName] = ("memory", dataSlot);
                    instructions.Add(CreatePushMemoryInstruction(streamVar.Index));
                    instructions.Add(new InstructionNode { Opcode = "awaitobj" });
                    instructions.Add(CreatePopMemoryInstruction(dataSlot));
                    return true;
                }

                if (TryParseCompileExpression(out var compileArgName) &&
                    vars.TryGetValue(compileArgName, out var dataVar) &&
                    (dataVar.Kind == "memory" || dataVar.Kind == "stream"))
                {
                    var resultSlot = memorySlotCounter++;
                    vars[targetName] = ("memory", resultSlot);
                    instructions.Add(CreatePushMemoryInstruction(dataVar.Index));
                    instructions.Add(CreatePushIntInstruction(1));
                    instructions.Add(CreateCallInstruction("compile"));
                    instructions.Add(CreatePopMemoryInstruction(resultSlot));
                    return true;
                }

                if (TryParseOriginExpression(out var shapeVarName) &&
                    vars.TryGetValue(shapeVarName, out var shapeVar) &&
                    shapeVar.Kind == "shape")
                {
                    instructions.Add(CreateCallInstruction("origin", CreateEntityCallParameter("shape", "shape", shapeVar.Index)));
                    instructions.Add(CreatePopMemoryInstruction(0));
                    vars[targetName] = ("memory", 0);
                    return true;
                }

                if (TryParseIntersectionExpression(line, out var shapeAName, out var shapeBText))
                {
                    if (!vars.TryGetValue(shapeAName, out var shapeA) || shapeA.Kind != "shape")
                        return true;

                    int? shapeBIndex = null;
                    if (vars.TryGetValue(shapeBText, out var shapeBVar) && shapeBVar.Kind == "shape")
                    {
                        shapeBIndex = shapeBVar.Index;
                    }
                    else
                    {
                        var tempShapeIndex = shapeCounter++;
                        instructions.AddRange(CompileShapeInit(tempShapeIndex, shapeBText, vars, ref vertexCounter));
                        shapeBIndex = tempShapeIndex;
                    }

                    if (shapeBIndex.HasValue)
                    {
                        instructions.Add(CreateCallInstruction(
                            "intersect",
                            CreateEntityCallParameter("shapeA", "shape", shapeA.Index),
                            CreateEntityCallParameter("shapeB", "shape", shapeBIndex.Value)));
                        instructions.Add(CreatePopMemoryInstruction(1));
                        vars[targetName] = ("memory", 1);
                    }
                    return true;
                }

                return true;
            }
            finally
            {
                _scanner = previous;
            }
        }

        private bool TryCompileFunctionCall(string line, Dictionary<string, (string Kind, int Index)> vars, List<InstructionNode> instructions)
        {
            var scanner = new Scanner(line);
            if (scanner.Current.Kind != TokenKind.Identifier)
                return false;
            var functionName = scanner.Scan().Value;
            if (scanner.Current.Kind != TokenKind.LParen)
                return false;
            scanner.Scan();
            if (scanner.Current.Kind != TokenKind.Identifier)
                return false;
            var argName = scanner.Scan().Value;
            if (scanner.Current.Kind != TokenKind.RParen)
                return false;
            scanner.Scan();
            while (scanner.Current.Kind == TokenKind.Semicolon)
                scanner.Scan();
            if (!scanner.Current.IsEndOfInput)
                return false;

            if (!string.Equals(functionName, "print", StringComparison.OrdinalIgnoreCase))
                return true;

            if (vars.TryGetValue(argName, out var argVar) && argVar.Kind == "memory")
            {
                instructions.Add(CreatePushMemoryInstruction(argVar.Index));
                instructions.Add(CreatePushIntInstruction(1));
                instructions.Add(CreateCallInstruction("print"));
            }
            return true;
        }

        private bool TryCompileMethodCall(string line, Dictionary<string, (string Kind, int Index)> vars, List<InstructionNode> instructions)
        {
            var scanner = new Scanner(line);
            if (scanner.Current.Kind != TokenKind.Identifier)
                return false;
            var objectName = scanner.Scan().Value;
            if (scanner.Current.Kind != TokenKind.Dot)
                return false;
            scanner.Scan();
            if (scanner.Current.Kind != TokenKind.Identifier)
                return false;
            var methodName = scanner.Scan().Value;
            if (scanner.Current.Kind != TokenKind.LParen)
                return false;
            scanner.Scan();

            string argText;
            if (scanner.Current.Kind == TokenKind.StringLiteral)
            {
                argText = scanner.Scan().Value;
            }
            else
            {
                var start = scanner.Current.Start;
                var end = start;
                while (!scanner.Current.IsEndOfInput && scanner.Current.Kind != TokenKind.RParen)
                {
                    end = scanner.Current.End;
                    scanner.Scan();
                }
                argText = start < end ? line.Substring(start, end - start).Trim() : "";
            }

            if (scanner.Current.Kind != TokenKind.RParen)
                return false;
            scanner.Scan();
            while (scanner.Current.Kind == TokenKind.Semicolon)
                scanner.Scan();
            if (!scanner.Current.IsEndOfInput)
                return false;

            if (!vars.TryGetValue(objectName, out var objVar))
                return true;

            instructions.Add(CreatePushMemoryInstruction(objVar.Index));
            instructions.Add(CreatePushStringInstruction(argText));
            instructions.Add(CreatePushIntInstruction(1));
            instructions.Add(new InstructionNode
            {
                Opcode = "callobj",
                Parameters = new List<ParameterNode>
                {
                    new FunctionNameParameterNode { FunctionName = methodName }
                }
            });
            return true;
        }

        private void EmitStreamDeclaration(string streamName, string elementType, Dictionary<string, (string Kind, int Index)> vars, ref int memorySlotCounter, List<InstructionNode> instructions)
        {
            var baseSlot = memorySlotCounter++;
            var streamSlot = memorySlotCounter++;
            vars[streamName] = ("stream", streamSlot);
            instructions.Add(CreatePushTypeInstruction("stream"));
            instructions.Add(new InstructionNode { Opcode = "def" });
            instructions.Add(CreatePopMemoryInstruction(baseSlot));
            instructions.Add(CreatePushMemoryInstruction(baseSlot));
            instructions.Add(CreatePushTypeInstruction(elementType));
            instructions.Add(CreatePushIntInstruction(1));
            instructions.Add(new InstructionNode { Opcode = "defgen" });
            instructions.Add(CreatePopMemoryInstruction(streamSlot));
        }

        private static InstructionNode CreateCallInstruction(string functionName, params FunctionParameterNode[] args)
        {
            var parameters = new List<ParameterNode>
            {
                new FunctionNameParameterNode { Name = "function", FunctionName = functionName }
            };
            foreach (var arg in args)
                parameters.Add(arg);
            return new InstructionNode { Opcode = "call", Parameters = parameters };
        }

        private static FunctionParameterNode CreateEntityCallParameter(string paramName, string entityType, long index)
        {
            return new FunctionParameterNode
            {
                Name = paramName,
                ParameterName = paramName,
                EntityType = entityType,
                Index = index
            };
        }

        private static InstructionNode CreatePushMemoryInstruction(long index)
        {
            return new InstructionNode
            {
                Opcode = "push",
                Parameters = new List<ParameterNode>
                {
                    new MemoryParameterNode { Name = "index", Index = index }
                }
            };
        }

        private static InstructionNode CreatePushStringInstruction(string value)
        {
            return new InstructionNode
            {
                Opcode = "push",
                Parameters = new List<ParameterNode>
                {
                    new StringParameterNode { Name = "string", Value = value }
                }
            };
        }

        private static InstructionNode CreatePushIntInstruction(long value)
        {
            return new InstructionNode
            {
                Opcode = "push",
                Parameters = new List<ParameterNode>
                {
                    new IndexParameterNode { Name = "int", Value = value }
                }
            };
        }

        private static InstructionNode CreatePushTypeInstruction(string typeName)
        {
            return new InstructionNode
            {
                Opcode = "push",
                Parameters = new List<ParameterNode>
                {
                    new TypeLiteralParameterNode { TypeName = typeName }
                }
            };
        }

        private static InstructionNode CreatePopMemoryInstruction(long index)
        {
            return new InstructionNode
            {
                Opcode = "pop",
                Parameters = new List<ParameterNode>
                {
                    new MemoryParameterNode { Name = "index", Index = index }
                }
            };
        }

        private bool IsDeclarationStatement(string line)
        {
            return TryParseStreamDeclaration(line, out _, out _) ||
                   TryParseEntityDeclaration(line, out _, out _, out _);
        }

        private bool TryParseVarKeywordOnly(string line)
        {
            var scanner = new Scanner(line);
            if (!IsIdentifier(scanner.Current, "var"))
                return false;
            scanner.Scan();
            while (scanner.Current.Kind == TokenKind.Semicolon)
                scanner.Scan();
            return scanner.Current.IsEndOfInput;
        }

        private bool TryStripInlineVarPrefix(string line, out string declarationLine)
        {
            declarationLine = "";
            var scanner = new Scanner(line);
            if (!IsIdentifier(scanner.Current, "var"))
                return false;
            scanner.Scan();
            if (scanner.Current.IsEndOfInput)
                return false;
            var start = scanner.Current.Start;
            var end = start;
            while (!scanner.Current.IsEndOfInput)
            {
                end = scanner.Current.End;
                scanner.Scan();
            }
            if (end <= start || start >= line.Length)
                return false;
            declarationLine = line.Substring(start, end - start).Trim();
            return !string.IsNullOrEmpty(declarationLine);
        }

        private bool TryParseStreamDeclaration(string line, out string streamName, out string elementType)
        {
            streamName = string.Empty;
            elementType = string.Empty;
            var previous = _scanner;
            _scanner = new Scanner(line);
            try
            {
                if (IsIdentifier(CurrentScanner.Current, "var"))
                    CurrentScanner.Scan();

                if (CurrentScanner.Current.Kind != TokenKind.Identifier)
                    return false;
                streamName = CurrentScanner.Scan().Value;

                if (CurrentScanner.Current.Kind != TokenKind.Colon || CurrentScanner.Watch(1)?.Kind != TokenKind.Assign)
                    return false;
                CurrentScanner.Scan();
                CurrentScanner.Scan();

                if (!IsIdentifier(CurrentScanner.Current, "stream"))
                    return false;
                CurrentScanner.Scan();

                if (CurrentScanner.Current.Kind != TokenKind.LessThan)
                    return false;
                CurrentScanner.Scan();
                if (CurrentScanner.Current.Kind != TokenKind.Identifier)
                    return false;
                elementType = CurrentScanner.Scan().Value.ToLowerInvariant();

                while (CurrentScanner.Current.Kind == TokenKind.Comma)
                {
                    CurrentScanner.Scan();
                    if (CurrentScanner.Current.Kind != TokenKind.Identifier)
                        return false;
                    CurrentScanner.Scan();
                }

                if (CurrentScanner.Current.Kind != TokenKind.GreaterThan)
                    return false;
                CurrentScanner.Scan();
                SkipSemicolon();
                return CurrentScanner.Current.IsEndOfInput;
            }
            finally
            {
                _scanner = previous;
            }
        }

        private bool TryParseEntityDeclaration(string sourceLine, out string varName, out string varType, out string initText)
        {
            varName = "";
            varType = "";
            initText = "";
            var previous = _scanner;
            _scanner = new Scanner(sourceLine);
            try
            {
                if (IsIdentifier(CurrentScanner.Current, "var"))
                    CurrentScanner.Scan();

                if (CurrentScanner.Current.Kind != TokenKind.Identifier)
                    return false;
                varName = CurrentScanner.Scan().Value;

                if (CurrentScanner.Current.Kind != TokenKind.Colon)
                    return false;
                CurrentScanner.Scan();
                if (CurrentScanner.Current.Kind != TokenKind.Identifier)
                    return false;
                varType = CurrentScanner.Scan().Value.ToLowerInvariant();
                if (varType != "vertex" && varType != "relation" && varType != "shape")
                    return false;
                if (CurrentScanner.Current.Kind != TokenKind.Assign)
                    return false;
                CurrentScanner.Scan();

                if (CurrentScanner.Current.IsEndOfInput)
                    return false;
                var start = CurrentScanner.Current.Start;
                var end = start;
                while (!CurrentScanner.Current.IsEndOfInput && CurrentScanner.Current.Kind != TokenKind.Semicolon)
                {
                    end = CurrentScanner.Current.End;
                    CurrentScanner.Scan();
                }
                initText = start < end ? sourceLine.Substring(start, end - start).Trim() : "";
                if (string.IsNullOrWhiteSpace(initText))
                    return false;

                SkipSemicolon();
                return CurrentScanner.Current.IsEndOfInput;
            }
            finally
            {
                _scanner = previous;
            }
        }

        private bool TryParseVertexObject(string initValue, out VertexInitSpec spec)
        {
            spec = new VertexInitSpec();
            var previous = _scanner;
            _scanner = new Scanner(initValue);
            try
            {
                if (CurrentScanner.Current.Kind != TokenKind.LBrace)
                    return false;
                CurrentScanner.Scan();

                while (!CurrentScanner.Current.IsEndOfInput && CurrentScanner.Current.Kind != TokenKind.RBrace)
                {
                    if (CurrentScanner.Current.Kind == TokenKind.Comma)
                    {
                        CurrentScanner.Scan();
                        continue;
                    }

                    if (IsIdentifier(CurrentScanner.Current, "DIM"))
                    {
                        CurrentScanner.Scan();
                        TryConsumeColon();
                        var dims = ParseLongArray();
                        if (dims.Count > 0)
                        {
                            spec.Dimensions.Clear();
                            spec.Dimensions.AddRange(dims);
                        }
                        continue;
                    }

                    if (IsIdentifier(CurrentScanner.Current, "W"))
                    {
                        CurrentScanner.Scan();
                        TryConsumeColon();
                        if (TryParseDoubleToken(out var weight))
                            spec.Weight = weight;
                        continue;
                    }

                    if (IsIdentifier(CurrentScanner.Current, "DATA"))
                    {
                        CurrentScanner.Scan();
                        TryConsumeColon();
                        if (IsIdentifier(CurrentScanner.Current, "BIN"))
                        {
                            CurrentScanner.Scan();
                            TryConsumeColon();
                            if (CurrentScanner.Current.Kind == TokenKind.StringLiteral)
                                spec.BinaryData = CurrentScanner.Scan().Value;
                        }
                        else if (CurrentScanner.Current.Kind == TokenKind.StringLiteral)
                        {
                            spec.TextData = CurrentScanner.Scan().Value;
                        }
                        continue;
                    }

                    CurrentScanner.Scan();
                }

                if (CurrentScanner.Current.Kind != TokenKind.RBrace)
                    return false;
                CurrentScanner.Scan();
                return CurrentScanner.Current.IsEndOfInput;
            }
            finally
            {
                _scanner = previous;
            }
        }

        private bool TryParseRelationObject(string initValue, out RelationInitSpec spec)
        {
            spec = new RelationInitSpec();
            var previous = _scanner;
            _scanner = new Scanner(initValue);
            try
            {
                if (CurrentScanner.Current.Kind != TokenKind.LBrace)
                    return false;
                CurrentScanner.Scan();

                if (CurrentScanner.Current.Kind == TokenKind.Identifier)
                    spec.From = CurrentScanner.Scan().Value;
                if (CurrentScanner.Current.Kind != TokenKind.Assign || CurrentScanner.Watch(1)?.Kind != TokenKind.GreaterThan)
                    return false;
                CurrentScanner.Scan();
                CurrentScanner.Scan();
                if (CurrentScanner.Current.Kind != TokenKind.Identifier)
                    return false;
                spec.To = CurrentScanner.Scan().Value;

                while (!CurrentScanner.Current.IsEndOfInput && CurrentScanner.Current.Kind != TokenKind.RBrace)
                {
                    if (CurrentScanner.Current.Kind == TokenKind.Comma)
                    {
                        CurrentScanner.Scan();
                        continue;
                    }
                    if (IsIdentifier(CurrentScanner.Current, "W"))
                    {
                        CurrentScanner.Scan();
                        TryConsumeColon();
                        if (TryParseDoubleToken(out var weight))
                            spec.Weight = weight;
                        continue;
                    }
                    CurrentScanner.Scan();
                }

                if (CurrentScanner.Current.Kind != TokenKind.RBrace)
                    return false;
                CurrentScanner.Scan();
                return CurrentScanner.Current.IsEndOfInput;
            }
            finally
            {
                _scanner = previous;
            }
        }

        private bool TryParseShapeObject(string initValue, out ShapeInitSpec spec)
        {
            spec = new ShapeInitSpec();
            var previous = _scanner;
            _scanner = new Scanner(initValue);
            try
            {
                if (CurrentScanner.Current.Kind != TokenKind.LBrace)
                    return false;
                CurrentScanner.Scan();

                if (IsIdentifier(CurrentScanner.Current, "VERT"))
                {
                    CurrentScanner.Scan();
                    TryConsumeColon();
                    if (CurrentScanner.Current.Kind != TokenKind.LBracket)
                        return false;
                    CurrentScanner.Scan();
                    while (!CurrentScanner.Current.IsEndOfInput && CurrentScanner.Current.Kind != TokenKind.RBracket)
                    {
                        if (CurrentScanner.Current.Kind == TokenKind.Comma)
                        {
                            CurrentScanner.Scan();
                            continue;
                        }
                        if (CurrentScanner.Current.Kind == TokenKind.LBrace && TryParseInlineVertex(out var inlineVertex))
                        {
                            spec.InlineVertices.Add(inlineVertex);
                            continue;
                        }
                        CurrentScanner.Scan();
                    }
                    if (CurrentScanner.Current.Kind != TokenKind.RBracket)
                        return false;
                    CurrentScanner.Scan();
                }
                else if (CurrentScanner.Current.Kind == TokenKind.LBracket)
                {
                    CurrentScanner.Scan();
                    while (!CurrentScanner.Current.IsEndOfInput && CurrentScanner.Current.Kind != TokenKind.RBracket)
                    {
                        if (CurrentScanner.Current.Kind == TokenKind.Comma)
                        {
                            CurrentScanner.Scan();
                            continue;
                        }
                        if (CurrentScanner.Current.Kind == TokenKind.Identifier)
                            spec.VertexNames.Add(CurrentScanner.Scan().Value);
                        else
                            CurrentScanner.Scan();
                    }
                    if (CurrentScanner.Current.Kind != TokenKind.RBracket)
                        return false;
                    CurrentScanner.Scan();
                }
                else
                {
                    return false;
                }

                while (!CurrentScanner.Current.IsEndOfInput && CurrentScanner.Current.Kind != TokenKind.RBrace)
                    CurrentScanner.Scan();
                if (CurrentScanner.Current.Kind != TokenKind.RBrace)
                    return false;
                CurrentScanner.Scan();
                return CurrentScanner.Current.IsEndOfInput;
            }
            finally
            {
                _scanner = previous;
            }
        }

        private bool TryParseInlineVertex(out InlineVertexSpec spec)
        {
            spec = new InlineVertexSpec();
            if (CurrentScanner.Current.Kind != TokenKind.LBrace)
                return false;
            CurrentScanner.Scan();

            while (!CurrentScanner.Current.IsEndOfInput && CurrentScanner.Current.Kind != TokenKind.RBrace)
            {
                if (CurrentScanner.Current.Kind == TokenKind.Comma)
                {
                    CurrentScanner.Scan();
                    continue;
                }
                if (IsIdentifier(CurrentScanner.Current, "DIM"))
                {
                    CurrentScanner.Scan();
                    TryConsumeColon();
                    var dims = ParseLongArray();
                    if (dims.Count > 0)
                    {
                        spec.Dimensions.Clear();
                        spec.Dimensions.AddRange(dims);
                    }
                    continue;
                }
                if (IsIdentifier(CurrentScanner.Current, "W"))
                {
                    CurrentScanner.Scan();
                    TryConsumeColon();
                    if (TryParseDoubleToken(out var weight))
                        spec.Weight = weight;
                    continue;
                }
                CurrentScanner.Scan();
            }

            if (CurrentScanner.Current.Kind != TokenKind.RBrace)
                return false;
            CurrentScanner.Scan();
            return true;
        }

        private List<long> ParseLongArray()
        {
            var values = new List<long>();
            if (CurrentScanner.Current.Kind != TokenKind.LBracket)
                return values;
            CurrentScanner.Scan();
            while (!CurrentScanner.Current.IsEndOfInput && CurrentScanner.Current.Kind != TokenKind.RBracket)
            {
                if (CurrentScanner.Current.Kind == TokenKind.Comma)
                {
                    CurrentScanner.Scan();
                    continue;
                }
                if ((CurrentScanner.Current.Kind == TokenKind.Number || CurrentScanner.Current.Kind == TokenKind.Float) &&
                    long.TryParse(CurrentScanner.Current.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var num))
                {
                    values.Add(num);
                }
                CurrentScanner.Scan();
            }
            if (CurrentScanner.Current.Kind == TokenKind.RBracket)
                CurrentScanner.Scan();
            return values;
        }

        private bool TryParseDoubleToken(out double value)
        {
            value = 0d;
            if (CurrentScanner.Current.Kind != TokenKind.Number && CurrentScanner.Current.Kind != TokenKind.Float)
                return false;
            var ok = double.TryParse(CurrentScanner.Current.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
            CurrentScanner.Scan();
            return ok;
        }

        private bool TryParseVaultReadExpression(out string vaultVarName, out string tokenKey)
        {
            vaultVarName = "";
            tokenKey = "";
            var pos = CurrentScanner.Save();
            if (CurrentScanner.Current.Kind != TokenKind.Identifier) return false;
            vaultVarName = CurrentScanner.Scan().Value;
            if (CurrentScanner.Current.Kind != TokenKind.Dot || !IsIdentifier(CurrentScanner.Watch(1), "read"))
            {
                CurrentScanner.Restore(pos);
                return false;
            }
            CurrentScanner.Scan();
            CurrentScanner.Scan();
            if (CurrentScanner.Current.Kind != TokenKind.LParen)
            {
                CurrentScanner.Restore(pos);
                return false;
            }
            CurrentScanner.Scan();
            if (CurrentScanner.Current.Kind != TokenKind.StringLiteral)
            {
                CurrentScanner.Restore(pos);
                return false;
            }
            tokenKey = CurrentScanner.Scan().Value;
            if (CurrentScanner.Current.Kind != TokenKind.RParen)
            {
                CurrentScanner.Restore(pos);
                return false;
            }
            CurrentScanner.Scan();
            SkipSemicolon();
            var success = CurrentScanner.Current.IsEndOfInput;
            if (!success) CurrentScanner.Restore(pos);
            return success;
        }

        private bool TryParseAwaitExpression(out string variableName)
        {
            variableName = "";
            var pos = CurrentScanner.Save();
            if (!IsIdentifier(CurrentScanner.Current, "await"))
                return false;
            CurrentScanner.Scan();
            if (CurrentScanner.Current.Kind != TokenKind.Identifier)
            {
                CurrentScanner.Restore(pos);
                return false;
            }
            variableName = CurrentScanner.Scan().Value;
            SkipSemicolon();
            var success = CurrentScanner.Current.IsEndOfInput;
            if (!success) CurrentScanner.Restore(pos);
            return success;
        }

        private bool TryParseCompileExpression(out string variableName)
        {
            variableName = "";
            var pos = CurrentScanner.Save();
            if (!IsIdentifier(CurrentScanner.Current, "compile"))
                return false;
            CurrentScanner.Scan();
            if (CurrentScanner.Current.Kind != TokenKind.LParen)
            {
                CurrentScanner.Restore(pos);
                return false;
            }
            CurrentScanner.Scan();
            if (CurrentScanner.Current.Kind != TokenKind.Identifier)
            {
                CurrentScanner.Restore(pos);
                return false;
            }
            variableName = CurrentScanner.Scan().Value;
            if (CurrentScanner.Current.Kind != TokenKind.RParen)
            {
                CurrentScanner.Restore(pos);
                return false;
            }
            CurrentScanner.Scan();
            SkipSemicolon();
            var success = CurrentScanner.Current.IsEndOfInput;
            if (!success) CurrentScanner.Restore(pos);
            return success;
        }

        private bool TryParseOriginExpression(out string shapeVarName)
        {
            shapeVarName = "";
            var pos = CurrentScanner.Save();
            if (!(CurrentScanner.Current.Kind == TokenKind.RBracket || CurrentScanner.Current.Value == "]"))
                return false;
            CurrentScanner.Scan();
            if (CurrentScanner.Current.Kind != TokenKind.Identifier)
            {
                CurrentScanner.Restore(pos);
                return false;
            }
            shapeVarName = CurrentScanner.Scan().Value;
            SkipSemicolon();
            var success = CurrentScanner.Current.IsEndOfInput;
            if (!success) CurrentScanner.Restore(pos);
            return success;
        }

        private bool TryParseIntersectionExpression(string sourceLine, out string shapeAName, out string shapeBText)
        {
            shapeAName = "";
            shapeBText = "";
            var pos = CurrentScanner.Save();
            if (CurrentScanner.Current.Kind != TokenKind.Identifier)
                return false;
            shapeAName = CurrentScanner.Scan().Value;
            if (CurrentScanner.Current.Value != "|")
            {
                CurrentScanner.Restore(pos);
                return false;
            }
            CurrentScanner.Scan();
            if (CurrentScanner.Current.IsEndOfInput)
            {
                CurrentScanner.Restore(pos);
                return false;
            }
            var start = CurrentScanner.Current.Start;
            var end = start;
            while (!CurrentScanner.Current.IsEndOfInput && CurrentScanner.Current.Kind != TokenKind.Semicolon)
            {
                end = CurrentScanner.Current.End;
                CurrentScanner.Scan();
            }
            shapeBText = start < end ? sourceLine.Substring(start, end - start).Trim() : "";
            var success = !string.IsNullOrWhiteSpace(shapeAName) && !string.IsNullOrWhiteSpace(shapeBText);
            if (!success)
                CurrentScanner.Restore(pos);
            return success;
        }

        private static bool TryParseProcedureName(string line, out string procedureName)
        {
            procedureName = "";
            var scanner = new Scanner(line);
            if (scanner.Current.Kind != TokenKind.Identifier)
                return false;
            procedureName = scanner.Scan().Value;
            while (scanner.Current.Kind == TokenKind.Semicolon)
                scanner.Scan();
            return scanner.Current.IsEndOfInput;
        }

        private static bool IsIdentifier(Token token, string value) =>
            token.Kind == TokenKind.Identifier && string.Equals(token.Value, value, StringComparison.OrdinalIgnoreCase);

        private static bool IsIdentifier(Token? token, string value) =>
            token.HasValue && token.Value.Kind == TokenKind.Identifier && string.Equals(token.Value.Value, value, StringComparison.OrdinalIgnoreCase);

        private void SkipSemicolon()
        {
            while (CurrentScanner.Current.Kind == TokenKind.Semicolon)
                CurrentScanner.Scan();
        }

        private void TryConsumeColon()
        {
            if (CurrentScanner.Current.Kind == TokenKind.Colon)
                CurrentScanner.Scan();
        }
    }
}
