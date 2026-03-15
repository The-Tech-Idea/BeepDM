using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Attributes;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Pipelines.Observability;
using AlertEvent = TheTechIdea.Beep.Pipelines.Observability.AlertEvent;

namespace TheTechIdea.Beep.Pipelines.Engine.BuiltIn.Notifiers
{
    /// <summary>
    /// Sends alert notifications via SMTP email.
    ///
    /// Config parameters:
    /// <list type="bullet">
    ///   <item><c>SmtpHost</c>       — SMTP server hostname (required)</item>
    ///   <item><c>SmtpPort</c>       — port, default 587</item>
    ///   <item><c>UseTls</c>         — enable STARTTLS, default true</item>
    ///   <item><c>Username</c>       — SMTP auth username (supports <c>${env:VAR}</c>)</item>
    ///   <item><c>Password</c>       — SMTP auth password (supports <c>${env:VAR}</c>)</item>
    ///   <item><c>From</c>           — sender address</item>
    ///   <item><c>To</c>             — JSON array of recipient addresses, or comma-separated</item>
    ///   <item><c>Subject</c>        — subject template (token-substituted)</item>
    ///   <item><c>BodyTemplate</c>   — body template (token-substituted)</item>
    /// </list>
    /// </summary>
    [PipelinePlugin(
        "beep.notify.email",
        "Email Notifier",
        PipelinePluginType.Notifier,
        Category = "Notify",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class EmailNotifier : IPipelineNotifier
    {
        public string PluginId    => "beep.notify.email";
        public string DisplayName => "Email Notifier";
        public string Description => "Sends alert notifications via SMTP email.";

        private string       _smtpHost    = string.Empty;
        private int          _smtpPort    = 587;
        private bool         _useTls      = true;
        private string       _username    = string.Empty;
        private string       _password    = string.Empty;
        private string       _from        = string.Empty;
        private List<string> _to          = new();
        private string       _subject     = "[{Severity}] Pipeline {PipelineName} — {AlertRule}";
        private string       _bodyTemplate = "Alert: {Message}\nRun: {RunId}\nPipeline: {PipelineName}\nFired: {FiredAt}";

        // ── IPipelinePlugin ────────────────────────────────────────────────────

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef { Name = "SmtpHost",      Type = ParamType.String,  IsRequired = true  },
            new PipelineParameterDef { Name = "SmtpPort",      Type = ParamType.Integer, IsRequired = false, DefaultValue = "587" },
            new PipelineParameterDef { Name = "UseTls",        Type = ParamType.Boolean, IsRequired = false, DefaultValue = "true" },
            new PipelineParameterDef { Name = "Username",      Type = ParamType.String,  IsRequired = false },
            new PipelineParameterDef { Name = "Password",      Type = ParamType.String,  IsRequired = false },
            new PipelineParameterDef { Name = "From",          Type = ParamType.String,  IsRequired = true  },
            new PipelineParameterDef { Name = "To",            Type = ParamType.String,  IsRequired = true  },
            new PipelineParameterDef { Name = "Subject",       Type = ParamType.String,  IsRequired = false },
            new PipelineParameterDef { Name = "BodyTemplate",  Type = ParamType.String,  IsRequired = false }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("SmtpHost",     out var h  )) _smtpHost     = Resolve(h);
            if (parameters.TryGetValue("From",         out var f  )) _from         = Resolve(f);
            if (parameters.TryGetValue("Username",     out var u  )) _username     = Resolve(u);
            if (parameters.TryGetValue("Password",     out var pw )) _password     = Resolve(pw);
            if (parameters.TryGetValue("Subject",      out var s  )) _subject      = Resolve(s);
            if (parameters.TryGetValue("BodyTemplate", out var bt )) _bodyTemplate = Resolve(bt);

            if (parameters.TryGetValue("SmtpPort", out var p) &&
                int.TryParse(p?.ToString(), out int pi)) _smtpPort = pi;

            if (parameters.TryGetValue("UseTls", out var tls) &&
                bool.TryParse(tls?.ToString(), out bool tlsb)) _useTls = tlsb;

            if (parameters.TryGetValue("To", out var to))
            {
                string toStr = Resolve(to);
                _to = toStr.StartsWith("[", StringComparison.Ordinal)
                    ? (System.Text.Json.JsonSerializer.Deserialize<List<string>>(toStr) ?? new List<string>())
                    : toStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(x => x.Trim()).ToList();
            }
        }

        // ── IPipelineNotifier ──────────────────────────────────────────────────

        public async Task NotifyAsync(AlertEvent alertEvent, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(_smtpHost) || _to.Count == 0) return;

            string subject = ApplyTokens(_subject, alertEvent);
            string body    = ApplyTokens(_bodyTemplate, alertEvent);

            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                EnableSsl             = _useTls,
                DeliveryMethod        = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrEmpty(_username))
                client.Credentials = new NetworkCredential(_username, _password);

            using var msg = new MailMessage
            {
                From    = new MailAddress(_from),
                Subject = subject,
                Body    = body,
                IsBodyHtml = false
            };
            foreach (var addr in _to)
                msg.To.Add(addr);

            await client.SendMailAsync(msg, token).ConfigureAwait(false);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>Resolves <c>${env:VAR_NAME}</c> references to environment variables.</summary>
        private static string Resolve(object? value)
        {
            string s = value?.ToString() ?? "";
            if (s.StartsWith("${env:", StringComparison.OrdinalIgnoreCase) && s.EndsWith("}"))
            {
                string varName = s.Substring(6, s.Length - 7);
                return Environment.GetEnvironmentVariable(varName) ?? s;
            }
            return s;
        }

        private static string ApplyTokens(string template, AlertEvent evt)
            => template
                .Replace("{Severity}",     evt.Severity.ToString())
                .Replace("{PipelineName}", evt.PipelineName)
                .Replace("{AlertRule}",    evt.RuleName)
                .Replace("{RunId}",        evt.RunId ?? "")
                .Replace("{Message}",      evt.Message)
                .Replace("{FiredAt}",      evt.FiredAtUtc.ToString("O"));
    }
}
