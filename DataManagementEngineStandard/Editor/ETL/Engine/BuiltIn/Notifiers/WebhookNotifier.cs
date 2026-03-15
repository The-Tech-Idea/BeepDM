using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
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
    /// Fires an HTTP webhook when an alert triggers.
    /// Supports Slack, Teams, and generic JSON webhooks.
    ///
    /// Config parameters:
    /// <list type="bullet">
    ///   <item><c>Url</c>             — webhook URL (required; supports <c>${env:VAR}</c>)</item>
    ///   <item><c>Method</c>          — "POST" | "PUT", default POST</item>
    ///   <item><c>Headers</c>         — JSON object of additional headers</item>
    ///   <item><c>PayloadTemplate</c> — JSON body template with <c>{Token}</c> substitutions</item>
    ///   <item><c>HmacSecret</c>      — optional HMAC-SHA256 secret for signing (supports <c>${env:VAR}</c>)</item>
    ///   <item><c>TimeoutSeconds</c>  — request timeout, default 15</item>
    /// </list>
    /// </summary>
    [PipelinePlugin(
        "beep.notify.webhook",
        "Webhook Notifier",
        PipelinePluginType.Notifier,
        Category = "Notify",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class WebhookNotifier : IPipelineNotifier
    {
        public string PluginId    => "beep.notify.webhook";
        public string DisplayName => "Webhook Notifier";
        public string Description => "Fires an HTTP webhook when an alert triggers.";

        private string _url             = string.Empty;
        private string _method          = "POST";
        private Dictionary<string, string> _headers = new();
        private string _payloadTemplate = @"{{""text"":""{Message}""}}";
        private string _hmacSecret      = string.Empty;
        private int    _timeoutSeconds  = 15;

        private static readonly HttpClient _http = new();

        // ── IPipelinePlugin ────────────────────────────────────────────────────

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef { Name = "Url",             Type = ParamType.String,  IsRequired = true  },
            new PipelineParameterDef { Name = "Method",          Type = ParamType.String,  IsRequired = false, DefaultValue = "POST" },
            new PipelineParameterDef { Name = "Headers",         Type = ParamType.Json,    IsRequired = false },
            new PipelineParameterDef { Name = "PayloadTemplate", Type = ParamType.String,  IsRequired = false },
            new PipelineParameterDef { Name = "HmacSecret",      Type = ParamType.String,  IsRequired = false },
            new PipelineParameterDef { Name = "TimeoutSeconds",  Type = ParamType.Integer, IsRequired = false, DefaultValue = "15" }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("Url",             out var u))  _url             = Resolve(u);
            if (parameters.TryGetValue("Method",          out var m))  _method          = m?.ToString() ?? "POST";
            if (parameters.TryGetValue("PayloadTemplate", out var pt)) _payloadTemplate = pt?.ToString() ?? _payloadTemplate;
            if (parameters.TryGetValue("HmacSecret",      out var hs)) _hmacSecret      = Resolve(hs);

            if (parameters.TryGetValue("TimeoutSeconds", out var t) &&
                int.TryParse(t?.ToString(), out int ti)) _timeoutSeconds = ti;

            if (parameters.TryGetValue("Headers", out var h) && h != null)
            {
                string raw = h.ToString()!;
                if (raw.TrimStart().StartsWith("{", StringComparison.Ordinal))
                {
                    var dict = System.Text.Json.JsonSerializer.Deserialize
                        <Dictionary<string, string>>(raw);
                    if (dict != null) _headers = dict;
                }
            }
        }

        // ── IPipelineNotifier ──────────────────────────────────────────────────

        public async Task NotifyAsync(AlertEvent alertEvent, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(_url)) return;

            string payload = ApplyTokens(_payloadTemplate, alertEvent);

            using var request = new HttpRequestMessage(
                new HttpMethod(_method.ToUpperInvariant()),
                _url);

            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            foreach (var (k, v) in _headers)
                request.Headers.TryAddWithoutValidation(k, v);

            if (!string.IsNullOrEmpty(_hmacSecret))
            {
                string sig = ComputeHmac(_hmacSecret, payload);
                request.Headers.TryAddWithoutValidation("X-Beep-Signature", $"sha256={sig}");
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

            await _http.SendAsync(request, cts.Token).ConfigureAwait(false);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

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
                .Replace("{PipelineName}", EscapeJson(evt.PipelineName))
                .Replace("{AlertRule}",    EscapeJson(evt.RuleName))
                .Replace("{RunId}",        evt.RunId ?? "")
                .Replace("{Message}",      EscapeJson(evt.Message))
                .Replace("{FiredAt}",      evt.FiredAtUtc.ToString("O"));

        private static string EscapeJson(string s)
            => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");

        private static string ComputeHmac(string secret, string payload)
        {
            byte[] keyBytes  = Encoding.UTF8.GetBytes(secret);
            byte[] dataBytes = Encoding.UTF8.GetBytes(payload);
            using var hmac = new HMACSHA256(keyBytes);
            byte[] hash = hmac.ComputeHash(dataBytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
