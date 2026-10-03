using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor.Defaults.Helpers;
using TheTechIdea.Beep.Editor.Importing;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    internal static class RequiredConfigurationRule
    {
        private static void Check()
        {
            RequiredDefaultResolution.Current?.Token.ThrowIfCancellationRequested();
            if (RequiredDefaultResolution.Current?.Failed == true) Deny();
        }

        private static void Deny() => throw new ImportTransformationException(ImportTransformationStage.Defaults);

        internal static object Resolve(string rule, IPassedArgs parameters)
        {
            Check();
            var text = rule.Trim();
            var open = text.IndexOf('(');
            var colon = text.IndexOf(':');
            var colonStyle = colon >= 0 && (open < 0 || colon < open);
            var op = text.Substring(0, colonStyle ? colon : open).ToUpperInvariant();
            var key = RequiredBuiltInRule.RequireAtom(colonStyle ? text.Substring(colon + 1)
                : RequiredBuiltInRule.SplitArguments(text.Substring(open + 1, text.Length - open - 2))[0], nonempty: true);
            ValidateKey(key);
            var sourceName = op switch
            {
                "CONFIG" or "CONFIGURATIONVALUE" or "APPSETTING" or "SETTING" => "AppSettings",
                "APPCONFIG" => "AppConfig",
                "WEBCONFIG" => "WebConfig",
                "CONNECTIONSTRING" => "ConnectionStrings",
                _ => throw new ImportTransformationException(ImportTransformationStage.Defaults)
            };
            var objects = parameters?.Objects;
            Check();
            if (objects?.Count > 10000) Deny();
            var matches = objects?.Where(item => item != null && string.Equals(item.Name, sourceName,
                StringComparison.OrdinalIgnoreCase)).ToArray() ?? Array.Empty<ObjectItem>();
            if (matches.Length > 1) Deny();
            if (matches.Length == 1) return ReadMap(matches[0].obj, key);

            // Declared host namespaces never fall through to unrelated environment/editor/row data.
            if (sourceName is "AppConfig" or "WebConfig") Deny();
            if (key.Contains('=')) Deny();
            if (sourceName == "AppSettings") return ReadEnvironment("APPSETTING_" + key)
                ?? throw new ImportTransformationException(ImportTransformationStage.Defaults);
            var first = ReadEnvironment("ConnectionStrings__" + key);
            var second = ReadEnvironment("CONNECTIONSTRING_" + key);
            if (first != null && second != null && !string.Equals(first, second, StringComparison.Ordinal)) Deny();
            return first ?? second ?? throw new ImportTransformationException(ImportTransformationStage.Defaults);
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > 1024 || key.Any(char.IsControl)) Deny();
        }

        private static string ReadEnvironment(string name)
        {
            Check();
            var value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
            Check();
            return value == null ? null : (string)DefaultValueHelper.CopyLiteralRequired(value);
        }

        private static string ReadMap(object value, string key)
        {
            Check();
            if (value is not IReadOnlyDictionary<string, string> source)
                throw new ImportTransformationException(ImportTransformationStage.Defaults);
            var count = source.Count;
            Check();
            if (count is < 0 or > 10000) Deny();
            var captured = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var visited = 0;
            long bytes = 0;
            IEnumerator<KeyValuePair<string, string>> cursor = null;
            try
            {
                Check();
                cursor = source.GetEnumerator();
                Check();
                if (cursor == null) Deny();
                while (true)
                {
                    Check();
                    var more = cursor.MoveNext();
                    Check();
                    if (!more) break;
                    if (++visited > 10000) Deny();
                    var pair = cursor.Current;
                    Check();
                    ValidateKey(pair.Key);
                    if (pair.Value == null) Deny();
                    var copy = (string)DefaultValueHelper.CopyLiteralRequired(pair.Value);
                    bytes = checked(bytes + 64L + pair.Key.Length * 2L + copy.Length * 2L);
                    if (bytes > 16L * 1024 * 1024 || !captured.TryAdd(pair.Key, copy)) Deny();
                }
            }
            finally { cursor?.Dispose(); }
            // Disposal and Count/enumerator callbacks can report failure or cancel this scope.
            Check();
            if (captured.Count != count || !captured.TryGetValue(key, out var result))
                throw new ImportTransformationException(ImportTransformationStage.Defaults);
            return result;
        }
    }
}
