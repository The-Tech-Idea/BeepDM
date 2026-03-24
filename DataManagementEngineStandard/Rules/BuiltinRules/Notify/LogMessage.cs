using System;
using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Notify
{
    /// <summary>
    /// Logs a structured notification message to the BeepDM diagnostic log via
    /// <c>IDMEEditor.AddLogMessage</c> (if available) or falls back to <see cref="Console"/>.
    /// Parameters:
    ///   <c>Message</c>    — message template (supports {Key} tokens from parameters).
    ///   <c>Level</c>      — "Info"|"Warning"|"Error" (default "Info").
    ///   <c>Source</c>     — log source label (default "RulesEngine").
    /// Optional <c>IDMEEditor</c> — when present, uses native logging.
    /// </summary>
    [Rule(ruleKey: "Notify.LogMessage", ParserKey = "RulesParser", RuleName = "LogMessage")]
    public sealed class LogMessage : IRule
    {
        public string RuleText { get; set; } = "Notify.LogMessage";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            parameters ??= new Dictionary<string, object>();
            var output = new Dictionary<string, object>();

            if (!parameters.TryGetValue("Message", out var msgRaw) || msgRaw == null)
            {
                output["Error"] = "Missing required parameter: Message";
                return (output, false);
            }

            string level  = "Info";
            string source = "RulesEngine";
            if (parameters.TryGetValue("Level",  out var lvRaw)  && lvRaw  != null) level  = lvRaw.ToString()!;
            if (parameters.TryGetValue("Source", out var srcRaw) && srcRaw != null) source = srcRaw.ToString()!;

            // Resolve {Token} placeholders in message
            string message = msgRaw.ToString()!;
            foreach (var kv in parameters)
            {
                string placeholder = "{" + kv.Key + "}";
                if (message.Contains(placeholder, StringComparison.Ordinal))
                    message = message.Replace(placeholder, kv.Value?.ToString() ?? "null",
                                              StringComparison.Ordinal);
            }

            try
            {
                if (parameters.TryGetValue("IDMEEditor", out var edRaw) &&
                    edRaw is TheTechIdea.Beep.Editor.IDMEEditor dm)
                {
                    var errLevel = level.ToUpperInvariant() == "ERROR"
                        ? Errors.Failed : Errors.Ok;
                    dm.AddLogMessage(source, message, DateTime.UtcNow, -1, null, errLevel);
                }
                else
                {
                    Console.WriteLine($"[{DateTime.UtcNow:o}] [{level}] [{source}] {message}");
                }
            }
            catch
            {
                Console.WriteLine($"[{DateTime.UtcNow:o}] [{level}] [{source}] {message}");
            }

            output["Result"]  = true;
            output["Message"] = message;
            return (output, true);
        }
    }
}
