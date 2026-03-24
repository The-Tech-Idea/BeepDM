using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Security
{
    /// <summary>
    /// Hashes <c>Value</c> using the specified algorithm.
    /// Parameters: <c>Value</c> (string), <c>Algorithm</c> ("SHA256"|"SHA512"|"MD5", default "SHA256").
    /// Optional <c>Salt</c> (string appended before hashing), <c>Encoding</c> ("hex"|"base64", default "hex").
    /// </summary>
    [Rule(ruleKey: "Security.HashValue", ParserKey = "RulesParser", RuleName = "HashValue")]
    public sealed class HashValue : IRule
    {
        public string RuleText { get; set; } = "Security.HashValue";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null || !parameters.TryGetValue("Value", out var rawValue))
            {
                output["Error"] = "Missing required parameter: Value";
                return (output, null);
            }

            string algorithm = "SHA256";
            if (parameters.TryGetValue("Algorithm", out var algRaw) && algRaw != null)
                algorithm = algRaw.ToString()!.ToUpperInvariant();

            string salt = string.Empty;
            if (parameters.TryGetValue("Salt", out var saltRaw) && saltRaw != null)
                salt = saltRaw.ToString()!;

            string encoding = "hex";
            if (parameters.TryGetValue("Encoding", out var encRaw) &&
                encRaw?.ToString()?.ToLowerInvariant() == "base64")
                encoding = "base64";

            byte[] input = Encoding.UTF8.GetBytes((rawValue?.ToString() ?? string.Empty) + salt);

            byte[] hash;
            try
            {
                hash = algorithm switch
                {
                    "SHA256" => SHA256.HashData(input),
                    "SHA512" => SHA512.HashData(input),
#pragma warning disable CA5351 // MD5 is permitted only for non-security use; caller assumes responsibility
                    "MD5"    => MD5.HashData(input),
#pragma warning restore CA5351
                    _        => throw new NotSupportedException($"Algorithm '{algorithm}' not supported. Use SHA256, SHA512, or MD5.")
                };
            }
            catch (NotSupportedException ex)
            {
                output["Error"] = ex.Message;
                return (output, null);
            }

            string res = encoding == "base64"
                ? Convert.ToBase64String(hash)
                : Convert.ToHexString(hash).ToLowerInvariant();

            output["Result"]    = res;
            output["Algorithm"] = algorithm;
            return (output, res);
        }
    }
}
