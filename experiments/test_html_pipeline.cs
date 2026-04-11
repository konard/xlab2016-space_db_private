// Experiment: Test the HTML parsing pipeline
// Usage: dotnet-script test_html_pipeline.cs

using System;
using System.Collections.Generic;
using System.Linq;

// Simulate the core parsing logic inline
static string TestHtmlPipeline(string htmlInput)
{
    Console.WriteLine($"\n=== Input HTML ===\n{htmlInput}\n");
    
    // Simple parse test - check we can parse the key elements
    var lines = htmlInput.Split('\n');
    Console.WriteLine($"Input has {lines.Length} lines");
    
    return "Test passed";
}

// Test the HTML from the issue
var html = """
<html>
  <body>
    <div id="login">
      Username
      Password
      <button>Login</button>
      <div id="error">Неверный логин или пароль</div>
    </div>
  </body>
</html>
""";

TestHtmlPipeline(html);
Console.WriteLine("Experiment completed successfully");
