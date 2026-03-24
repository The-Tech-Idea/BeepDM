using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Notify
{
    /// <summary>
    /// Sends an HTTP POST webhook with a JSON payload built from <c>Payload</c> parameters.
    /// Parameters:
    ///   <c>Url</c>          — target URL (required).
    ///   <c>Payload</c>      — semicolon-separated "key=value" pairs OR a JSON string.
    ///   <c>Token</c>        — optional Bearer token for Authorization header.
    ///   <c>TimeoutSeconds</c> — request timeout (default "10").
    /// Returns <c>true</c> on HTTP 2xx; writes <c>StatusCode</c> to output.
    /// NOTE: Uses <c>HttpClient</c>. For production, inject a shared client via IoC.
    /// </summary>
    [Rule(ruleKey: "Notify.SendWebhook", ParserKey = "RulesParser", RuleName = "SendWebhook")]
    public sealed class SendWebhook : IRule
    {
        // Shared HttpClient — rules are often called in tight loops.
        private static readonly HttpClient _http = new HttpClient();

        public string RuleText { get; set; } = "Notify.SendWebhook";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null || !parameters.TryGetValue("Url", out var urlRaw) || urlRaw == null)
            {
                output["Error"] = "Missing required parameter: Url";
                return (output, false);
            }

            int timeout = 10;
            if (parameters.TryGetValue("TimeoutSeconds", out var toRaw))
                int.TryParse(toRaw?.ToString(), out timeout);

            // Build payload dictionary
            var payload = new Dictionary<string, object?>();
            if (parameters.TryGetValue("Payload", out var payloadRaw))
            {
                string ps = payloadRaw?.ToString() ?? string.Empty;
                if (ps.TrimStart().StartsWith('{'))
                {
                    try
                    {
                        var deserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(ps);
                        if (deserialized != null)
                            foreach (var kv in deserialized)
                                payload[kv.Key] = kv.Value;
                    }
                    catch { payload["raw"] = ps; }
                }
                else
                {
                    foreach (string entry in ps.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        int eq = entry.IndexOf('=', StringComparison.Ordinal);
                        if (eq < 0) continue;
                        payload[entry[..eq].Trim()] = entry[(eq + 1)..].Trim();
                    }
                }
            }

            string json = JsonSerializer.Serialize(payload);
            using var req = new HttpRequestMessage(HttpMethod.Post, urlRaw.ToString())
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            if (parameters.TryGetValue("Token", out var tokenRaw) && tokenRaw != null)
                req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {tokenRaw}");

            try
            {
                using var cts = new System.Threading.CancellationTokenSource(
                                   TimeSpan.FromSeconds(timeout));
                var task = _http.SendAsync(req, cts.Token);
                task.Wait(cts.Token);
                var resp = task.Result;

                bool ok = resp.IsSuccessStatusCode;
                output["Result"]     = ok;
                output["StatusCode"] = (int)resp.StatusCode;
                return (output, ok);
            }
            catch (Exception ex)
            {
                output["Error"]  = ex.Message;
                output["Result"] = false;
                return (output, false);
            }
        }
    }
}
