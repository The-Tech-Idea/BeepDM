using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Attributes;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Engine.BuiltIn.Schedulers
{
    /// <summary>
    /// Fires a pipeline on a CRON schedule.
    /// Supports standard 5-part CRON expressions: <c>min hour dom mon dow</c>.
    /// Also accepts 6-part expressions with a leading seconds field.
    ///
    /// Config parameters:
    /// <list type="bullet">
    ///   <item><c>CronExpression</c> — e.g. <c>"0 2 * * *"</c> (required)</item>
    ///   <item><c>PipelineId</c>     — pipeline to trigger (injected by SchedulerHost)</item>
    ///   <item><c>TimeZone</c>       — IANA or Windows tz name, default <c>"UTC"</c></item>
    ///   <item><c>MaxMissedRuns</c>  — if host was down, fire at most N missed executions (default 1)</item>
    /// </list>
    /// </summary>
    [PipelinePlugin(
        "beep.schedule.cron",
        "Cron Scheduler",
        PipelinePluginType.Scheduler,
        Category = "Schedule",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class CronScheduler : IPipelineScheduler
    {
        public string PluginId    => "beep.schedule.cron";
        public string DisplayName => "Cron Scheduler";
        public string Description => "Fires a pipeline on a CRON schedule.";

        private string     _expression  = string.Empty;
        private string     _pipelineId  = string.Empty;
        private TimeZoneInfo _tz        = TimeZoneInfo.Utc;
        private int        _maxMissed   = 1;

        private CancellationTokenSource? _cts;

        public event EventHandler<PipelineTriggerArgs>? Triggered;

        // ── IPipelinePlugin ────────────────────────────────────────────────────

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef { Name = "CronExpression", Type = ParamType.String,  IsRequired = true  },
            new PipelineParameterDef { Name = "PipelineId",     Type = ParamType.String,  IsRequired = true  },
            new PipelineParameterDef { Name = "TimeZone",       Type = ParamType.String,  IsRequired = false, DefaultValue = "UTC" },
            new PipelineParameterDef { Name = "MaxMissedRuns",  Type = ParamType.Integer, IsRequired = false, DefaultValue = "1"   }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("CronExpression", out var e)) _expression = e?.ToString() ?? "";
            if (parameters.TryGetValue("PipelineId",     out var p)) _pipelineId = p?.ToString() ?? "";
            if (parameters.TryGetValue("MaxMissedRuns",  out var m) &&
                int.TryParse(m?.ToString(), out int mi))              _maxMissed  = mi;

            if (parameters.TryGetValue("TimeZone", out var tz))
            {
                string tzStr = tz?.ToString() ?? "UTC";
                try
                {
                    _tz = tzStr == "UTC"
                        ? TimeZoneInfo.Utc
                        : TimeZoneInfo.FindSystemTimeZoneById(tzStr);
                }
                catch { _tz = TimeZoneInfo.Utc; }
            }
        }

        // ── IPipelineScheduler ─────────────────────────────────────────────────

        public Task StartAsync(CancellationToken token)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _ = Task.Run(() => RunLoopAsync(_cts.Token), CancellationToken.None);
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            _cts?.Cancel();
            return Task.CompletedTask;
        }

        // ── Core loop ──────────────────────────────────────────────────────────

        private async Task RunLoopAsync(CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(_expression)) return;

            CronExpr? cron;
            try { cron = CronExpr.Parse(_expression); }
            catch { return; /* invalid expression — silent fail */ }

            DateTime lastFired = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz);

            while (!token.IsCancellationRequested)
            {
                var now  = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz);
                var next = cron.GetNextOccurrence(now);

                if (next == DateTime.MaxValue) break;  // no more firings

                var delay = next - now;
                if (delay > TimeSpan.Zero)
                {
                    try
                    {
                        await Task.Delay(delay, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { break; }
                }

                // Fire
                Triggered?.Invoke(this, new PipelineTriggerArgs
                {
                    PipelineId    = _pipelineId,
                    TriggerSource = "cron",
                    Parameters    = new Dictionary<string, object>
                    {
                        ["__cron_expression"] = _expression,
                        ["__fired_at"]        = DateTime.UtcNow.ToString("O")
                    }
                });

                lastFired = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz);
            }
        }

        // ── Embedded CRON parser ────────────────────────────────────────────────

        private sealed class CronExpr
        {
            private readonly HashSet<int> _seconds;   // null = any (5-part cron has no seconds)
            private readonly HashSet<int> _minutes;
            private readonly HashSet<int> _hours;
            private readonly HashSet<int> _doms;      // day-of-month 1-31
            private readonly HashSet<int> _months;    // 1-12
            private readonly HashSet<int> _dows;      // 0-7 (0 and 7 = Sunday)
            private readonly bool         _hasSeconds;

            private CronExpr(HashSet<int>? seconds, HashSet<int> minutes, HashSet<int> hours,
                HashSet<int> doms, HashSet<int> months, HashSet<int> dows)
            {
                _seconds    = seconds ?? new HashSet<int>();
                _minutes    = minutes;
                _hours      = hours;
                _doms       = doms;
                _months     = months;
                _dows       = dows;
                _hasSeconds = seconds != null;
            }

            public static CronExpr Parse(string expr)
            {
                var parts = expr.Trim().Split(' ',
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 5)
                    return new CronExpr(null,
                        ParseField(parts[0], 0, 59),
                        ParseField(parts[1], 0, 23),
                        ParseField(parts[2], 1, 31),
                        ParseField(parts[3], 1, 12),
                        ParseDow(parts[4]));

                if (parts.Length == 6)
                    return new CronExpr(
                        ParseField(parts[0], 0, 59),
                        ParseField(parts[1], 0, 59),
                        ParseField(parts[2], 0, 23),
                        ParseField(parts[3], 1, 31),
                        ParseField(parts[4], 1, 12),
                        ParseDow(parts[5]));

                throw new FormatException($"CRON expression must have 5 or 6 fields, got {parts.Length}.");
            }

            private static HashSet<int> ParseField(string s, int min, int max)
            {
                var result = new HashSet<int>();
                foreach (var part in s.Split(','))
                {
                    if (part == "*")
                    {
                        for (int i = min; i <= max; i++) result.Add(i);
                    }
                    else if (part.StartsWith("*/", StringComparison.Ordinal))
                    {
                        int step = int.Parse(part.Substring(2));
                        for (int i = min; i <= max; i += step) result.Add(i);
                    }
                    else if (part.Contains('-'))
                    {
                        var bounds = part.Split('-');
                        int a = int.Parse(bounds[0]);
                        int b = int.Parse(bounds[1]);
                        for (int i = a; i <= b; i++) result.Add(i);
                    }
                    else
                    {
                        result.Add(int.Parse(part));
                    }
                }
                return result;
            }

            private static HashSet<int> ParseDow(string s)
            {
                // Normalise Sunday: treat 7 as 0
                var raw = ParseField(s, 0, 7);
                if (raw.Remove(7)) raw.Add(0);
                return raw;
            }

            public DateTime GetNextOccurrence(DateTime after)
            {
                // Start 1 unit after 'after'
                var dt = _hasSeconds
                    ? after.AddSeconds(1)
                    : after.AddMinutes(1).AddSeconds(-after.Second);

                // Truncate to the resolution we care about
                dt = _hasSeconds
                    ? new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second)
                    : new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);

                var limit = after.AddYears(4);

                while (dt <= limit)
                {
                    if (!_months.Contains(dt.Month))
                    {
                        dt = new DateTime(dt.Year, dt.Month, 1).AddMonths(1);
                        continue;
                    }
                    if (!_doms.Contains(dt.Day) || !_dows.Contains((int)dt.DayOfWeek))
                    {
                        dt = dt.Date.AddDays(1);
                        continue;
                    }
                    if (!_hours.Contains(dt.Hour))
                    {
                        dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0).AddHours(1);
                        continue;
                    }
                    if (!_minutes.Contains(dt.Minute))
                    {
                        dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0).AddMinutes(1);
                        continue;
                    }
                    if (_hasSeconds && !_seconds.Contains(dt.Second))
                    {
                        dt = dt.AddSeconds(1);
                        continue;
                    }
                    return dt;
                }

                return DateTime.MaxValue;
            }
        }
    }
}
