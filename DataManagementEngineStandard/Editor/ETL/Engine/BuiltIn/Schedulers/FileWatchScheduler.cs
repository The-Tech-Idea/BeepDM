using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Attributes;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Engine.BuiltIn.Schedulers
{
    /// <summary>
    /// Fires a pipeline whenever files matching a pattern appear in (or change inside) a directory.
    ///
    /// Config parameters:
    /// <list type="bullet">
    ///   <item><c>WatchPath</c>    — directory to watch (required)</item>
    ///   <item><c>PipelineId</c>   — pipeline to trigger (injected by SchedulerHost)</item>
    ///   <item><c>FilePattern</c>  — e.g. <c>"*.csv"</c>, default <c>"*"</c></item>
    ///   <item><c>Recursive</c>    — watch sub-directories, default <c>false</c></item>
    ///   <item><c>TriggerOn</c>    — <c>"Created"</c> | <c>"Changed"</c> | <c>"Both"</c>, default <c>"Created"</c></item>
    ///   <item><c>Debounce_ms</c>  — milliseconds between the first event and the trigger (default 2000)</item>
    ///   <item><c>StabilityMs</c>  — additional wait to confirm the file is fully written (default 500)</item>
    ///   <item><c>PassFilePath</c> — inject <c>__trigger_file</c> into Parameters, default <c>true</c></item>
    /// </list>
    /// </summary>
    [PipelinePlugin(
        "beep.schedule.filewatch",
        "File Watch Scheduler",
        PipelinePluginType.Scheduler,
        Category = "Schedule",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class FileWatchScheduler : IPipelineScheduler
    {
        public string PluginId    => "beep.schedule.filewatch";
        public string DisplayName => "File Watch Scheduler";
        public string Description => "Fires a pipeline when files are created or changed in a directory.";

        private string _watchPath   = string.Empty;
        private string _pipelineId  = string.Empty;
        private string _pattern     = "*";
        private bool   _recursive   = false;
        private string _triggerOn   = "Created";
        private int    _debounceMs  = 2000;
        private int    _stabilityMs = 500;
        private bool   _passPath    = true;

        private FileSystemWatcher? _watcher;
        private readonly object _timersLock = new object();
        private readonly Dictionary<string, System.Threading.Timer> _debounceTimers = new();

        public event EventHandler<PipelineTriggerArgs>? Triggered;

        // ── IPipelinePlugin ────────────────────────────────────────────────────

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef { Name = "WatchPath",    Type = ParamType.FilePath, IsRequired = true  },
            new PipelineParameterDef { Name = "PipelineId",   Type = ParamType.String,   IsRequired = true  },
            new PipelineParameterDef { Name = "FilePattern",  Type = ParamType.String,   IsRequired = false, DefaultValue = "*" },
            new PipelineParameterDef { Name = "Recursive",    Type = ParamType.Boolean,  IsRequired = false, DefaultValue = "false" },
            new PipelineParameterDef { Name = "TriggerOn",    Type = ParamType.String,   IsRequired = false, DefaultValue = "Created" },
            new PipelineParameterDef { Name = "Debounce_ms",  Type = ParamType.Integer,  IsRequired = false, DefaultValue = "2000" },
            new PipelineParameterDef { Name = "StabilityMs",  Type = ParamType.Integer,  IsRequired = false, DefaultValue = "500" },
            new PipelineParameterDef { Name = "PassFilePath", Type = ParamType.Boolean,  IsRequired = false, DefaultValue = "true" }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("WatchPath",   out var wp))  _watchPath  = wp?.ToString()  ?? "";
            if (parameters.TryGetValue("PipelineId",  out var p))   _pipelineId = p?.ToString()   ?? "";
            if (parameters.TryGetValue("FilePattern", out var fp))  _pattern    = fp?.ToString()  ?? "*";
            if (parameters.TryGetValue("TriggerOn",   out var t))   _triggerOn  = t?.ToString()   ?? "Created";

            if (parameters.TryGetValue("Recursive",    out var r) &&
                bool.TryParse(r?.ToString(), out bool rb))           _recursive  = rb;

            if (parameters.TryGetValue("Debounce_ms", out var d) &&
                int.TryParse(d?.ToString(), out int di))             _debounceMs = di;

            if (parameters.TryGetValue("StabilityMs", out var s) &&
                int.TryParse(s?.ToString(), out int si))             _stabilityMs = si;

            if (parameters.TryGetValue("PassFilePath", out var pp) &&
                bool.TryParse(pp?.ToString(), out bool ppb))         _passPath   = ppb;
        }

        // ── IPipelineScheduler ─────────────────────────────────────────────────

        public Task StartAsync(CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(_watchPath) || !Directory.Exists(_watchPath))
                return Task.CompletedTask;

            _watcher = new FileSystemWatcher(_watchPath, _pattern)
            {
                IncludeSubdirectories = _recursive,
                EnableRaisingEvents   = false,
                NotifyFilter          = NotifyFilters.FileName | NotifyFilters.LastWrite
            };

            string ton = _triggerOn.ToUpperInvariant();
            if (ton == "CREATED" || ton == "BOTH")
                _watcher.Created += OnFileEvent;
            if (ton == "CHANGED" || ton == "BOTH")
                _watcher.Changed += OnFileEvent;

            // Register cancellation cleanup
            token.Register(() => StopAsync());

            _watcher.EnableRaisingEvents = true;
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Created -= OnFileEvent;
                _watcher.Changed -= OnFileEvent;
                _watcher.Dispose();
                _watcher = null;
            }

            lock (_timersLock)
            {
                foreach (var t in _debounceTimers.Values) t.Dispose();
                _debounceTimers.Clear();
            }

            return Task.CompletedTask;
        }

        // ── Internal event handling ────────────────────────────────────────────

        private void OnFileEvent(object sender, FileSystemEventArgs e)
        {
            string path = e.FullPath;

            lock (_timersLock)
            {
                if (_debounceTimers.TryGetValue(path, out var existing))
                {
                    // Reset the debounce timer
                    existing.Change(_debounceMs, Timeout.Infinite);
                    return;
                }

                var timer = new System.Threading.Timer(
                    _ => DebounceElapsed(path),
                    null,
                    _debounceMs,
                    Timeout.Infinite);

                _debounceTimers[path] = timer;
            }
        }

        private void DebounceElapsed(string path)
        {
            // Remove the timer
            lock (_timersLock)
            {
                if (_debounceTimers.TryGetValue(path, out var t))
                {
                    t.Dispose();
                    _debounceTimers.Remove(path);
                }
            }

            // Optionally wait for file write stability
            if (_stabilityMs > 0)
                Thread.Sleep(_stabilityMs);

            var parms = new Dictionary<string, object>();
            if (_passPath)
            {
                parms["__trigger_file"]     = path;
                parms["__trigger_filename"] = Path.GetFileName(path);
            }

            Triggered?.Invoke(this, new PipelineTriggerArgs
            {
                PipelineId    = _pipelineId,
                TriggerSource = "filewatch",
                Parameters    = parms
            });
        }
    }
}
