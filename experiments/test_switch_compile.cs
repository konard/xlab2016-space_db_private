// Quick experiment script to test switch compilation
// Run with: dotnet-script or dotnet fsi
using System;
using System.Threading.Tasks;
using Magic.Kernel.Compilation;

// Minimal reproduction of client_claw.agi issue
var source = @"@AGI 0.0.1;

program clients_claw;
system samples;
module claw;

procedure call(data) {
    var authentication := data.authentication;

    if !authentication.isAuthenticated return;

    var command := data.command;
    var socket1 := socket;

    switch command {
        if ""hello_world""
            print(""Hello world"");
    }
}

entrypoint {
    Main;
}";

var compiler = new Compiler();
var result = await compiler.CompileAsync(source);
if (result.Success)
    Console.WriteLine("SUCCESS");
else
    Console.WriteLine($"FAILURE: {result.ErrorMessage}");
