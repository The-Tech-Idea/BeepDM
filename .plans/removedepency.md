# Dependency Removal Plan

## 1. Goal
Remove all third-party logging (`Serilog` and its sinks) from the `DataManagementEngineStandard` component, replacing them with native .NET functionality. The NuGet SDK dependencies will be kept intact.

## 2. Packages to Remove
Open `c:\Users\f_ald\source\repos\The-Tech-Idea\BeepDM\DataManagementEngineStandard\DataManagementEngine.csproj` and remove the following PackageReferences:

### Serilog
* `Serilog`
* `Serilog.AspNetCore`
* `Serilog.Extensions.Logging`
* `Serilog.Sinks.Console`
* `Serilog.Sinks.File`

## 3. Implementation Steps for Logger (`DMLogger.cs`)
The current logger (`DMLogger.cs`) implements both `Serilog.ILogger` and `Microsoft.Extensions.Logging.ILogger`.
1. **Remove Serilog interface**: Remove `Serilog.ILogger` inheritance and its specific implementation methods (e.g., `Write(LogEvent logEvent)`).
2. **Remove Serilog Configuration**: Remove `new LoggerConfiguration().MinimumLevel.Debug()...` initialization.
3. **Use Default ILogger**: Keep `Microsoft.Extensions.Logging.ILogger` as the primary interface. In the constructor or configuration routine, instantiate a native factory via `LoggerFactory.Create(builder => { builder.AddConsole(); builder.SetMinimumLevel(LogLevel.Debug); })`.
4. **Implement File Logging**: Since standard .NET does not have a built-in file sink without third-party libraries, implement a lightweight `ILoggerProvider` for a thread-safe custom file writer (writing to `log.txt`), or utilize logging extensions.
5. **Update Usings**: Remove `using Serilog;` and `using Serilog.Events;`. Update `MapToSerilogLevel` to map natively or eliminate entirely since `Microsoft.Extensions.Logging.LogLevel` will be the default.

## 4. Verification
1. Run `dotnet build DataManagementEngineStandard`.
2. Ensure log outputs (both Console and standard File) replicate current behavior natively.
3. Verify that `DataManagementEngine.csproj` utilizes 0 logging dependencies outside standard .NET (`Microsoft.Extensions.*` is native standard).
