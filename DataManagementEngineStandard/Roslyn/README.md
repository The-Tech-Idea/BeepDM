# Roslyn Compiler

## Overview

Runtime code compilation support using Microsoft.CodeAnalysis (Roslyn) for dynamic code generation and advanced tooling scenarios.

## Key Files

- `RoslynCompiler.cs` - Main compiler implementation

## Features

- Runtime C# code compilation
- Dynamic assembly generation
- Integration with BeepDM's dynamic type system
- Support for runtime script execution

## Usage

```csharp
var compiler = new RoslynCompiler();
var result = compiler.CompileCode(@"
    public class DynamicClass
    {
        public string GetMessage() => \"Hello from compiled code!\";
    }
");

if (result.Success)
{
    var instance = result.Assembly.CreateInstance("DynamicClass");
    var message = instance.GetType().GetMethod("GetMessage").Invoke(instance, null);
    Console.WriteLine(message);
}
```

## How It Fits

Used by:
- Dynamic entity type generation
- Advanced tooling scenarios
- Runtime script execution
- Custom resolver compilation

## Related Documentation

- [Core Architecture](../Docs/CoreArchitecture.md)
- [Tools](../Docs/Tools.md)
