using System;
using System.IO;
using TheTechIdea.Beep.Editor.Defaults.Helpers;
using TheTechIdea.Beep.Editor.Importing;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    internal static class RequiredEnvironmentRule
    {
        private static void Check()
        {
            RequiredDefaultResolution.Current?.Token.ThrowIfCancellationRequested();
            if (RequiredDefaultResolution.Current?.Failed == true)
                throw new ImportTransformationException(ImportTransformationStage.Defaults);
        }

        internal static object Resolve(string rule)
        {
            Check();
            var op = rule.Trim().ToUpperInvariant();
            if (op is "TEMP" or "TEMPPATH")
            {
                var path = Path.GetTempPath();
                Check();
                return DefaultValueHelper.CopyLiteralRequired(path);
            }
            if (op is "SYSTEMPATH" or "USERPATH")
                return Read("PATH", op == "SYSTEMPATH" ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User);

            var open = rule.IndexOf('(');
            var colon = rule.IndexOf(':');
            if (colon >= 0 && (open < 0 || colon < open)) return Read(RequiredBuiltInRule.RequireAtom(rule.Substring(colon + 1), nonempty: true),
                EnvironmentVariableTarget.Process);
            var args = RequiredBuiltInRule.SplitArguments(rule.Substring(open + 1, rule.Length - open - 2));
            var name = RequiredBuiltInRule.RequireAtom(args[0], nonempty: true);
            var target = args.Length == 1 ? "PROCESS" : RequiredBuiltInRule.RequireAtom(args[1], nonempty: true).ToUpperInvariant();
            return Read(name, target switch
            {
                "PROCESS" => EnvironmentVariableTarget.Process,
                "USER" => EnvironmentVariableTarget.User,
                "MACHINE" or "SYSTEM" => EnvironmentVariableTarget.Machine,
                _ => throw new ImportTransformationException(ImportTransformationStage.Defaults)
            });
        }

        private static object Read(string name, EnvironmentVariableTarget target)
        {
            Check();
            if (name.Contains('=') || target != EnvironmentVariableTarget.Process && !OperatingSystem.IsWindows())
                throw new ImportTransformationException(ImportTransformationStage.Defaults);
            var value = Environment.GetEnvironmentVariable(name, target);
            Check();
            return DefaultValueHelper.CopyLiteralRequired(value);
        }
    }
}
