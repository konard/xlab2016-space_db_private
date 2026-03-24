using Magic.Kernel.Compilation;
using System;

var parser = new InstructionParser();

Console.WriteLine("Test 1: push string: \"Time\" (with colon)");
try {
    var result1 = parser.Parse("push string: \"Time\"");
    Console.WriteLine($"  Opcode: {result1.Opcode}, Params: {result1.Parameters?.Count}");
    if (result1.Parameters?.Count > 0 && result1.Parameters[0] is StringParameterNode sp1)
        Console.WriteLine($"  Value: {sp1.Value}");
} catch (Exception ex) {
    Console.WriteLine($"  ERROR: {ex.Message}");
}

Console.WriteLine("Test 2: push string \"hello\" (without colon)");
try {
    var result2 = parser.Parse("push string \"hello\"");
    Console.WriteLine($"  Opcode: {result2.Opcode}, Params: {result2.Parameters?.Count}");
    if (result2.Parameters?.Count > 0 && result2.Parameters[0] is StringParameterNode sp2)
        Console.WriteLine($"  Value: {sp2.Value}");
} catch (Exception ex) {
    Console.WriteLine($"  ERROR: {ex.Message}");
}
