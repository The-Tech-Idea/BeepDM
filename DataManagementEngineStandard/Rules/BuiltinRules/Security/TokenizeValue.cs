using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Security
{
    /// <summary>
    /// Replaces <c>Value</c> with a deterministic, reversible token scoped by <c>VaultKey</c>.
    /// Tokens are stored in an in-process vault keyed by <c>VaultKey</c>.
    /// <para>Parameters:</para>
    /// <list type="bullet">
    ///   <item><c>Value</c> (string) — original sensitive value.</item>
    ///   <item><c>VaultKey</c> (string) — logical vault/scope identifier (e.g. "creditCard", "ssn").</item>
    ///   <item><c>Operation</c> ("tokenize"|"detokenize", default "tokenize").</item>
    ///   <item><c>Token</c> (string) — required when <c>Operation</c> is "detokenize".</item>
    /// </list>
    /// <para>Note: the default vault is in-process only and is not persistent.
    /// Replace <see cref="VaultStore"/> with a persistent implementation for production use.</para>
    /// </summary>
    [Rule(ruleKey: "Security.TokenizeValue", ParserKey = "RulesParser", RuleName = "TokenizeValue")]
    public sealed class TokenizeValue : IRule
    {
        // Static in-process vault: VaultKey → (token → original value)
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>>
            VaultStore = new();

        public string RuleText { get; set; } = "Security.TokenizeValue";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null || !parameters.TryGetValue("VaultKey", out var vkRaw))
            {
                output["Error"] = "Missing required parameter: VaultKey";
                return (output, null);
            }

            string vaultKey = vkRaw?.ToString() ?? "default";
            string operation = "tokenize";
            if (parameters.TryGetValue("Operation", out var opRaw))
                operation = opRaw?.ToString()?.ToLowerInvariant() ?? "tokenize";

            var vault = VaultStore.GetOrAdd(vaultKey, _ => new ConcurrentDictionary<string, string>());

            if (operation == "detokenize")
            {
                if (!parameters.TryGetValue("Token", out var tokenRaw) || tokenRaw == null)
                {
                    output["Error"] = "Parameter 'Token' is required for detokenize operation";
                    return (output, null);
                }
                string tok = tokenRaw.ToString()!;
                if (vault.TryGetValue(tok, out string? original))
                {
                    output["Result"] = original;
                    return (output, original);
                }
                output["Error"] = $"Token '{tok}' not found in vault '{vaultKey}'";
                return (output, null);
            }

            // tokenize
            if (!parameters.TryGetValue("Value", out var valRaw) || valRaw == null)
            {
                output["Error"] = "Missing required parameter: Value";
                return (output, null);
            }

            string value = valRaw.ToString()!;
            // Deterministic token: HMACSHA256 of value keyed by VaultKey, truncated to 16 hex chars.
            string token = ComputeToken(value, vaultKey);
            vault[token] = value;

            output["Result"] = token;
            output["Token"]  = token;
            return (output, token);
        }

        private static string ComputeToken(string value, string vaultKey)
        {
            byte[] key   = Encoding.UTF8.GetBytes(vaultKey.PadRight(32)[..32]);
            byte[] data  = Encoding.UTF8.GetBytes(value);
            using var hmac = new HMACSHA256(key);
            byte[] hash  = hmac.ComputeHash(data);
            return Convert.ToHexString(hash)[..16].ToLowerInvariant();
        }
    }
}
