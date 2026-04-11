// Run with: dotnet script show_v2_output.cs
// Or just include in a test
using System;
using System.IO;
using Magic.Kernel2.Compilation2;

var agiFile = "/tmp/gh-issue-solver-1775689387015/design/Space/samples/telegram_to_db.agi";
var source = File.ReadAllText(agiFile);
var compiler = new Compiler2(source);
var result = compiler.Compile();

foreach (var block in result.Blocks)
{
    Console.WriteLine($"=== {block.Name} ===");
    foreach (var cmd in block.Commands)
    {
        Console.WriteLine(FormatCmd(cmd));
    }
    Console.WriteLine();
}
