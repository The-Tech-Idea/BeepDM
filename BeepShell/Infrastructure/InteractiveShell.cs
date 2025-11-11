using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Shell.Commands;
using BeepShell.Infrastructure;

namespace TheTechIdea.Beep.Shell.Infrastructure
{
    /// <summary>
    /// Interactive REPL shell for BeepDM
    /// Maintains persistent DMEEditor instance and connections across commands
    /// </summary>
    public class InteractiveShell : IDisposable
    {
        private ShellServiceProvider _services;
        private IDMEEditor _editor;
        private readonly SessionState _sessionState;
        private bool _isRunning = true;
        private readonly RootCommand _rootCommand;
        private readonly List<IShellCommand> _loadedCommands = new();
        private readonly List<IShellWorkflow> _loadedWorkflows = new();
        private readonly List<IShellExtension> _loadedExtensions = new();
        private readonly IShellEventBus _eventBus;
        private readonly IPluginManager _pluginManager;
        private readonly Dictionary<string, string> _commandAliases = new();

        public InteractiveShell(string profileName)
        {
            _services = new ShellServiceProvider(profileName);
            _editor = _services.GetEditor();
            _sessionState = new SessionState(profileName);
            
            // Initialize event bus and plugin manager
            _eventBus = new ShellEventBus();
            _pluginManager = new PluginManager(_editor);
            
            // Load shell extensions from already-scanned types in AssemblyHandler
            LoadShellExtensions();
            
            // Build root command with all CLI commands and extensions
            _rootCommand = BuildRootCommand();
            
            // Setup built-in command aliases
            SetupDefaultAliases();
            
            // Publish shell started event
            _eventBus.PublishAsync(ShellEventType.ShellStarted).GetAwaiter().GetResult();
            
            AnsiConsole.MarkupLine($"[green]✓[/] Profile: [cyan]{profileName}[/]");
            AnsiConsole.MarkupLine($"[dim]Config: {_services.ConfigPath}[/]");
            
            if (_loadedCommands.Count > 0 || _loadedExtensions.Count > 0)
            {
                AnsiConsole.MarkupLine($"[green]✓[/] Loaded {_loadedCommands.Count} custom commands, {_loadedWorkflows.Count} workflows, {_loadedExtensions.Count} extensions");
            }
            
            AnsiConsole.WriteLine();
        }

        public int Run()
        {
            while (_isRunning)
            {
                try
                {
                    // Display prompt
                    var input = AnsiConsole.Prompt(
                        new TextPrompt<string>("[cyan]beep>[/]")
                            .AllowEmpty()
                    );

                    // Skip empty input
                    if (string.IsNullOrWhiteSpace(input))
                        continue;

                    // Add to history
                    _sessionState.AddToHistory(input);

                    // Parse and execute command
                    ExecuteCommand(input.Trim());
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
                }
            }

            return 0;
        }

        private void ExecuteCommand(string input)
        {
            var sw = Stopwatch.StartNew();

            // Handle shell-specific commands first
            if (HandleShellCommand(input))
            {
                return;
            }

            try
            {
                // Parse the input as command line arguments
                var args = ParseCommandLine(input);
                
                // Execute using the root command
                var result = _rootCommand.Invoke(args);
                
                sw.Stop();
                
                if (result != 0)
                {
                    AnsiConsole.MarkupLine($"[yellow]Command completed with code {result}[/]");
                }
                
                _sessionState.CommandExecuted(sw.Elapsed);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] {Markup.Escape(ex.Message)}");
                _sessionState.CommandFailed();
            }
        }

        private bool HandleShellCommand(string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            var command = parts[0].ToLower();

            switch (command)
            {
                case "exit":
                case "quit":
                case "q":
                    _isRunning = false;
                    AnsiConsole.MarkupLine("[cyan]Goodbye![/]");
                    return true;

                case "clear":
                case "cls":
                    AnsiConsole.Clear();
                    return true;

                case "help":
                case "?":
                    ShellCommands.ShowHelp(_loadedCommands);
                    return true;

                case "status":
                    ShellCommands.ShowStatus(_editor, _sessionState);
                    return true;

                case "connections":
                    ShellCommands.ShowConnections(_editor);
                    return true;

                case "datasources":
                    ShellCommands.ShowDataSources(_editor);
                    return true;

                case "history":
                    ShellCommands.ShowHistory(_sessionState);
                    return true;

                case "extensions":
                    ShowExtensions();
                    return true;

                case "workflows":
                    ShowWorkflows();
                    return true;

                case "profile":
                    if (parts.Length > 1 && parts[1] == "switch" && parts.Length > 2)
                    {
                        SwitchProfile(parts[2]);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[cyan]Current profile:[/] {_services.ProfileName}");
                        AnsiConsole.MarkupLine($"[dim]Use 'profile switch <name>' to change[/]");
                    }
                    return true;

                case "reload":
                    _services.ReloadConfiguration();
                    AnsiConsole.MarkupLine("[green]✓[/] Configuration reloaded");
                    return true;

                case "close":
                    if (parts.Length > 1)
                    {
                        CloseDataSource(parts[1]);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[yellow]Usage:[/] close <datasource-name>");
                    }
                    return true;

                case "plugin":
                    HandlePluginCommand(parts);
                    return true;

                case "events":
                    ShowEventSubscribers();
                    return true;

                case "alias":
                    HandleAliasCommand(parts);
                    return true;

                default:
                    // Check if it's a command alias
                    if (_commandAliases.TryGetValue(command, out var actualCommand))
                    {
                        var newInput = actualCommand + input.Substring(command.Length);
                        ExecuteCommand(newInput);
                        return true;
                    }
                    return false;
            }
        }

        private void SwitchProfile(string newProfile)
        {
            try
            {
                AnsiConsole.Status()
                    .Start($"Switching to profile '{newProfile}'...", ctx =>
                    {
                        // Close current connections
                        foreach (var ds in _editor.DataSources.ToList())
                        {
                            try
                            {
                                if (ds.ConnectionStatus == ConnectionState.Open)
                                {
                                    ds.Closeconnection();
                                }
                            }
                            catch { }
                        }

                        // Switch to new profile
                        _services.Dispose();
                        _services = new ShellServiceProvider(newProfile);
                        _editor = _services.GetEditor();
                        _sessionState.ProfileName = newProfile;
                    });

                AnsiConsole.MarkupLine($"[green]✓[/] Switched to profile: [cyan]{newProfile}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Failed to switch profile: {ex.Message}");
            }
        }

        private void CloseDataSource(string name)
        {
            try
            {
                var ds = _editor.DataSources.FirstOrDefault(d => 
                    d.DatasourceName.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[yellow]Data source '{name}' not found in active connections[/]");
                    return;
                }

                if (ds.ConnectionStatus == ConnectionState.Open)
                {
                    ds.Closeconnection();
                    AnsiConsole.MarkupLine($"[green]✓[/] Closed connection to '{name}'");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[dim]Connection '{name}' was already closed[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error closing connection: {ex.Message}");
            }
        }

        private string[] ParseCommandLine(string input)
        {
            // Simple command line parser
            var args = new List<string>();
            var currentArg = "";
            var inQuotes = false;

            for (int i = 0; i < input.Length; i++)
            {
                var c = input[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ' ' && !inQuotes)
                {
                    if (!string.IsNullOrEmpty(currentArg))
                    {
                        args.Add(currentArg);
                        currentArg = "";
                    }
                }
                else
                {
                    currentArg += c;
                }
            }

            if (!string.IsNullOrEmpty(currentArg))
            {
                args.Add(currentArg);
            }

            return args.ToArray();
        }

        private RootCommand BuildRootCommand()
        {
            // Import commands from CLI project (we'll reference the same command builders)
            var rootCommand = new RootCommand("BeepDM Interactive Shell");

            // Add core assembly management commands as base
            rootCommand.AddCommand(AssemblyCommands.Build(_editor));
            
            // Note: We'll need to import the CLI Commands namespace
            // For now, create a simple structure
            
            // TODO: Import actual commands from CLI project
            // rootCommand.AddCommand(CLI.Commands.ConfigCommands.Build());
            // rootCommand.AddCommand(CLI.Commands.DataSourceCommands.Build());
            // etc.

            // Add dynamically loaded commands
            foreach (var cmd in _loadedCommands)
            {
                try
                {
                    var command = cmd.BuildCommand();
                    rootCommand.AddCommand(command);
                    
                    // Add aliases for the command
                    if (cmd.Aliases != null && cmd.Aliases.Length > 0)
                    {
                        foreach (var alias in cmd.Aliases)
                        {
                            // Add alias to the command itself
                            command.AddAlias(alias);
                        }
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning:[/] Failed to load command '{cmd.CommandName}': {ex.Message}");
                }
            }

            return rootCommand;
        }

        private void LoadShellExtensions()
        {
            try
            {
                // First, scan the BeepShell assembly itself for built-in commands
                ScanCurrentAssemblyCommands();

                // Get already-instantiated loader extensions from AssemblyHandler
                // AssemblyHandler.ScanExtensions() already created and scanned these during LoadAllAssembly()
                var loaderExtensions = _editor.assemblyHandler.LoaderExtensionInstances;

                if (loaderExtensions == null || loaderExtensions.Count == 0)
                {
                    return;
                }

                AnsiConsole.Status()
                    .Start("Loading shell extensions...", ctx =>
                    {
                        // Find ShellExtensionScanner instances and get their discovered commands/workflows/extensions
                        foreach (var extension in loaderExtensions)
                        {
                            try
                            {
                                if (extension is ShellExtensionScanner shellScanner)
                                {
                                    // Get the INSTANCES that were already created during scanning
                                    var commands = shellScanner.Commands;
                                    var workflows = shellScanner.Workflows;
                                    var extensions = shellScanner.Extensions;

                                    // Initialize and load extension providers first
                                    foreach (var ext in extensions)
                                    {
                                        ext.Initialize(_editor);
                                        _loadedExtensions.Add(ext);

                                        // Get commands and workflows from the extension provider
                                        foreach (var cmd in ext.GetCommands())
                                        {
                                            cmd.Initialize(_editor);
                                            _loadedCommands.Add(cmd);
                                        }

                                        foreach (var wf in ext.GetWorkflows())
                                        {
                                            wf.Initialize(_editor);
                                            _loadedWorkflows.Add(wf);
                                        }
                                    }

                                    // Load standalone commands
                                    foreach (var cmd in commands)
                                    {
                                        cmd.Initialize(_editor);
                                        _loadedCommands.Add(cmd);
                                    }

                                    // Load standalone workflows
                                    foreach (var wf in workflows)
                                    {
                                        wf.Initialize(_editor);
                                        _loadedWorkflows.Add(wf);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[yellow]Warning:[/] Failed to process extension: {ex.Message}");
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error loading extensions:[/] {ex.Message}");
            }
        }

        public void Dispose()
        {
            try
            {
                // Publish stopping event
                _eventBus.PublishAsync(ShellEventType.ShellStopping).GetAwaiter().GetResult();
                
                // Cleanup extensions
                foreach (var ext in _loadedExtensions)
                {
                    try
                    {
                        ext.OnUnload();
                        ext.Cleanup();
                    }
                    catch { }
                }

                // Unload plugins
                // TODO: _pluginManager doesn't have UnloadAll() method - need to unload individually

                _services?.Dispose();
                
                // Clear event bus
                _eventBus?.Clear();
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        private void SetupDefaultAliases()
        {
            _commandAliases["ls"] = "datasources";
            _commandAliases["quit"] = "exit";
            _commandAliases["q"] = "exit";
            _commandAliases["h"] = "help";
            _commandAliases["stat"] = "status";
        }

        private void ScanCurrentAssemblyCommands()
        {
            try
            {
                var currentAssembly = typeof(InteractiveShell).Assembly;
                var commandTypes = currentAssembly.GetTypes()
                    .Where(t => !t.IsAbstract && 
                               !t.IsInterface && 
                               typeof(IShellCommand).IsAssignableFrom(t))
                    .ToList();

                foreach (var type in commandTypes)
                {
                    try
                    {
                        var command = (IShellCommand)Activator.CreateInstance(type)!;
                        command.Initialize(_editor);
                        _loadedCommands.Add(command);
                    }
                    catch (Exception ex)
                    {
                        _editor.Logger?.WriteLog($"Failed to load command {type.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _editor.Logger?.WriteLog($"Error scanning current assembly: {ex.Message}");
            }
        }

        private void HandlePluginCommand(string[] parts)
        {
            if (parts.Length < 2)
            {
                ShowPluginHelp();
                return;
            }

            var subCommand = parts[1].ToLower();

            switch (subCommand)
            {
                case "list":
                    ShowPlugins();
                    break;

                case "load":
                    if (parts.Length < 3)
                    {
                        AnsiConsole.MarkupLine("[yellow]Usage:[/] plugin load <path>");
                    }
                    else
                    {
                        LoadPlugin(parts[2]);
                    }
                    break;

                case "unload":
                    if (parts.Length < 3)
                    {
                        AnsiConsole.MarkupLine("[yellow]Usage:[/] plugin unload <plugin-id>");
                    }
                    else
                    {
                        UnloadPlugin(parts[2]);
                    }
                    break;

                case "reload":
                    if (parts.Length < 3)
                    {
                        AnsiConsole.MarkupLine("[yellow]Usage:[/] plugin reload <plugin-id>");
                    }
                    else
                    {
                        ReloadPlugin(parts[2]);
                    }
                    break;

                case "health":
                    if (parts.Length < 3)
                    {
                        ShowAllPluginHealth();
                    }
                    else
                    {
                        ShowPluginHealth(parts[2]);
                    }
                    break;

                default:
                    ShowPluginHelp();
                    break;
            }
        }

        private void ShowPluginHelp()
        {
            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.AddColumn("[cyan]Command[/]");
            table.AddColumn("[cyan]Description[/]");

            table.AddRow("plugin list", "Show all loaded plugins");
            table.AddRow("plugin load <path>", "Load a plugin from path");
            table.AddRow("plugin unload <id>", "Unload a plugin");
            table.AddRow("plugin reload <id>", "Reload a plugin");
            table.AddRow("plugin health [id]", "Show plugin health status");

            AnsiConsole.Write(table);
        }

        private void ShowPlugins()
        {
            var plugins = _pluginManager.LoadedPlugins;

            if (!plugins.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No plugins loaded[/]");
                return;
            }

            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.AddColumn("[cyan]ID[/]");
            table.AddColumn("[cyan]Name[/]");
            table.AddColumn("[cyan]Version[/]");
            table.AddColumn("[cyan]Hot Reload[/]");
            table.AddColumn("[cyan]Commands[/]");
            table.AddColumn("[cyan]Workflows[/]");

            foreach (var plugin in plugins)
            {
                var hotReload = plugin.SupportsHotReload ? "[green]✓[/]" : "[dim]✗[/]";
                table.AddRow(
                    plugin.PluginId,
                    plugin.ExtensionName,
                    plugin.Version,
                    hotReload,
                    plugin.GetCommands().Count().ToString(),
                    plugin.GetWorkflows().Count().ToString()
                );
            }

            AnsiConsole.Write(table);
        }

        private async void LoadPlugin(string path)
        {
            try
            {
                var result = await _pluginManager.LoadPluginAsync(path, isolated: true);
                
                if (result.Success)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] {result.Message}");
                    
                    // Publish event
                    await _eventBus.PublishAsync(ShellEventType.ExtensionLoaded, new ShellEventArgs
                    {
                        Data = { ["pluginId"] = result.Plugin.PluginId }
                    });
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] {result.Message}");
                    foreach (var error in result.Errors)
                    {
                        AnsiConsole.MarkupLine($"[dim]{error}[/]");
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
        }

        private async void UnloadPlugin(string pluginId)
        {
            try
            {
                var success = await _pluginManager.UnloadPluginAsync(pluginId);
                
                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Plugin unloaded: {pluginId}");
                    
                    // Publish event
                    await _eventBus.PublishAsync(ShellEventType.ExtensionUnloaded, new ShellEventArgs
                    {
                        Data = { ["pluginId"] = pluginId }
                    });
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]Failed to unload plugin: {pluginId}[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
        }

        private async void ReloadPlugin(string pluginId)
        {
            try
            {
                var result = await _pluginManager.ReloadPluginAsync(pluginId);
                
                if (result.Success)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] {result.Message}");
                    
                    // Publish event
                    await _eventBus.PublishAsync(ShellEventType.PluginReloaded, new ShellEventArgs
                    {
                        Data = { ["pluginId"] = pluginId }
                    });
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] {result.Message}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
        }

        private void ShowPluginHealth(string pluginId)
        {
            var plugin = _pluginManager.GetPlugin(pluginId);
            if (plugin == null)
            {
                AnsiConsole.MarkupLine($"[yellow]Plugin not found: {pluginId}[/]");
                return;
            }

            var health = plugin.GetHealthStatus();
            DisplayHealthStatus(plugin.ExtensionName, health);
        }

        private void ShowAllPluginHealth()
        {
            var plugins = _pluginManager.LoadedPlugins;
            
            if (!plugins.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No plugins loaded[/]");
                return;
            }

            foreach (var plugin in plugins)
            {
                var health = plugin.GetHealthStatus();
                DisplayHealthStatus(plugin.ExtensionName, health);
                AnsiConsole.WriteLine();
            }
        }

        private void DisplayHealthStatus(string name, PluginHealthStatus health)
        {
            var statusColor = health.IsHealthy ? "green" : "red";
            AnsiConsole.MarkupLine($"[{statusColor}]● {name}[/]: {health.Status}");
            
            if (health.Warnings.Any())
            {
                AnsiConsole.MarkupLine($"  [yellow]Warnings:[/]");
                foreach (var warning in health.Warnings)
                {
                    AnsiConsole.MarkupLine($"    - {warning}");
                }
            }
            
            if (health.Errors.Any())
            {
                AnsiConsole.MarkupLine($"  [red]Errors:[/]");
                foreach (var error in health.Errors)
                {
                    AnsiConsole.MarkupLine($"    - {error}");
                }
            }
        }

        private void ShowEventSubscribers()
        {
            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.AddColumn("[cyan]Event Type[/]");
            table.AddColumn("[cyan]Subscribers[/]");

            foreach (ShellEventType eventType in Enum.GetValues<ShellEventType>())
            {
                var count = ((ShellEventBus)_eventBus).GetSubscriberCount(eventType);
                if (count > 0)
                {
                    table.AddRow(eventType.ToString(), count.ToString());
                }
            }

            if (table.Rows.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No event subscribers[/]");
            }
            else
            {
                AnsiConsole.Write(table);
            }
        }

        private void HandleAliasCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                // Show all aliases
                ShowAliases();
            }
            else if (parts.Length == 2 && parts[1] == "clear")
            {
                _commandAliases.Clear();
                SetupDefaultAliases();
                AnsiConsole.MarkupLine("[green]✓[/] Aliases reset to defaults");
            }
            else if (parts.Length >= 3)
            {
                // Add/update alias
                var alias = parts[1];
                var command = string.Join(" ", parts.Skip(2));
                _commandAliases[alias] = command;
                AnsiConsole.MarkupLine($"[green]✓[/] Alias '{alias}' -> '{command}'");
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Usage:[/] alias [<name> <command>] | alias clear");
            }
        }

        private void ShowAliases()
        {
            if (!_commandAliases.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No aliases defined[/]");
                return;
            }

            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.AddColumn("[cyan]Alias[/]");
            table.AddColumn("[cyan]Command[/]");

            foreach (var kvp in _commandAliases.OrderBy(x => x.Key))
            {
                table.AddRow(kvp.Key, kvp.Value);
            }

            AnsiConsole.Write(table);
        }

        // Public API for extensions
        public IShellEventBus EventBus => _eventBus;
        public IPluginManager PluginManager => _pluginManager;

        private void ShowExtensions()
        {
            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.AddColumn("[cyan]Extension[/]");
            table.AddColumn("[cyan]Version[/]");
            table.AddColumn("[cyan]Author[/]");
            table.AddColumn("[cyan]Commands[/]");
            table.AddColumn("[cyan]Workflows[/]");

            foreach (var ext in _loadedExtensions)
            {
                var cmdCount = ext.GetCommands().Count();
                var wfCount = ext.GetWorkflows().Count();
                table.AddRow(
                    ext.ExtensionName,
                    ext.Version,
                    ext.Author,
                    cmdCount.ToString(),
                    wfCount.ToString()
                );
            }

            // Add standalone commands
            var standaloneCommands = _loadedCommands
                .Where(c => !_loadedExtensions.Any(e => e.GetCommands().Contains(c)))
                .ToList();

            if (standaloneCommands.Count > 0)
            {
                table.AddRow(
                    "[dim]Standalone Commands[/]",
                    "-",
                    "-",
                    standaloneCommands.Count.ToString(),
                    "0"
                );
            }

            AnsiConsole.Write(table);
        }

        private void ShowWorkflows()
        {
            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.AddColumn("[cyan]Workflow[/]");
            table.AddColumn("[cyan]Category[/]");
            table.AddColumn("[cyan]Description[/]");

            foreach (var wf in _loadedWorkflows)
            {
                table.AddRow(
                    wf.WorkflowName,
                    wf.Category,
                    wf.Description
                );
            }

            if (_loadedWorkflows.Count == 0)
            {
                AnsiConsole.MarkupLine("[dim]No workflows loaded[/]");
            }
            else
            {
                AnsiConsole.Write(table);
            }
        }
    }

    /// <summary>
    /// Tracks session state and statistics
    /// </summary>
    public class SessionState
    {
        public string ProfileName { get; set; }
        public DateTime StartTime { get; }
        public List<string> CommandHistory { get; }
        public int TotalCommands { get; private set; }
        public int SuccessfulCommands { get; private set; }
        public int FailedCommands { get; private set; }
        public TimeSpan TotalExecutionTime { get; private set; }

        public SessionState(string profileName)
        {
            ProfileName = profileName;
            StartTime = DateTime.Now;
            CommandHistory = new List<string>();
        }

        public void AddToHistory(string command)
        {
            CommandHistory.Add(command);
            if (CommandHistory.Count > 100)
            {
                CommandHistory.RemoveAt(0);
            }
        }

        public void CommandExecuted(TimeSpan executionTime)
        {
            TotalCommands++;
            SuccessfulCommands++;
            TotalExecutionTime += executionTime;
        }

        public void CommandFailed()
        {
            TotalCommands++;
            FailedCommands++;
        }

        public TimeSpan SessionDuration => DateTime.Now - StartTime;
    }
}
