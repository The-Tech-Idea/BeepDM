using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace TheTechIdea.Beep.Installer
{
    /// <summary>
    /// Evaluates installation conditions for conditional components.
    /// Supports OS version, architecture, registry checks, file existence, and custom commands.
    /// </summary>
    public static class InstallConditionEvaluator
    {
        public static bool Evaluate(InstallCondition condition)
        {
            if (condition == null) return true;
            return condition.Type switch
            {
                ConditionType.OsVersion => CheckOsVersion(condition.Operator, condition.Value),
                ConditionType.Architecture => CheckArchitecture(condition.Value),
                ConditionType.RegistryExists => CheckRegistryExists(condition.Value),
                ConditionType.RegistryValue => CheckRegistryValue(condition.Value, condition.Operator, condition.Value2),
                ConditionType.FileExists => CheckFileExists(condition.Value),
                ConditionType.DirectoryExists => CheckDirectoryExists(condition.Value),
                ConditionType.CommandReturns => CheckCommandReturns(condition.Value, condition.Operator, condition.Value2),
                ConditionType.IsAdmin => new System.Security.Principal.WindowsPrincipal(
                    System.Security.Principal.WindowsIdentity.GetCurrent())
                    .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator),
                ConditionType.AlwaysTrue => true,
                ConditionType.AlwaysFalse => false,
                _ => true
            };
        }

        public static bool EvaluateAll(List<InstallCondition>? conditions)
            => conditions == null || conditions.Count == 0 || conditions.All(Evaluate);

        public static bool EvaluateAny(List<InstallCondition>? conditions)
            => conditions == null || conditions.Count == 0 || conditions.Any(Evaluate);

        private static bool CheckOsVersion(string op, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            var osVer = Environment.OSVersion.Version;
            var target = new Version(value);
            return Compare(osVer, target, op);
        }

        private static bool CheckArchitecture(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            var arch = RuntimeInformation.OSArchitecture.ToString();
            return string.Equals(arch, value, StringComparison.OrdinalIgnoreCase);
        }

        private static bool CheckRegistryExists(string? keyPath)
        {
            if (string.IsNullOrWhiteSpace(keyPath)) return false;
            try { using var key = Registry.LocalMachine.OpenSubKey(keyPath); return key != null; }
            catch { return false; }
        }

        private static bool CheckRegistryValue(string? keyValuePath, string op, string? expectedValue)
        {
            if (string.IsNullOrWhiteSpace(keyValuePath)) return false;
            try
            {
                var parts = keyValuePath.Split('|');
                using var key = Registry.LocalMachine.OpenSubKey(parts[0]);
                if (key == null) return false;
                var actual = key.GetValue(parts.Length > 1 ? parts[1] : "")?.ToString();
                return string.Equals(actual, expectedValue, StringComparison.OrdinalIgnoreCase)
                    || (op == ">=" && CompareVersions(actual, expectedValue) >= 0);
            }
            catch { return false; }
        }

        private static bool CheckFileExists(string? path)
        {
            return !string.IsNullOrWhiteSpace(path) && File.Exists(
                Environment.ExpandEnvironmentVariables(path));
        }

        private static bool CheckDirectoryExists(string? path)
        {
            return !string.IsNullOrWhiteSpace(path) && Directory.Exists(
                Environment.ExpandEnvironmentVariables(path));
        }

        private static bool CheckCommandReturns(string? command, string op, string? expectedOutput)
        {
            if (string.IsNullOrWhiteSpace(command)) return false;
            try
            {
                var parts = command.Split(' ', 2);
                var p = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = parts[0], Arguments = parts.Length > 1 ? parts[1] : "",
                        RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
                    }
                };
                p.Start();
                var output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(10000);

                if (string.IsNullOrEmpty(expectedOutput)) return p.ExitCode == 0;
                return output.Contains(expectedOutput, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private static bool Compare(Version a, Version b, string op) => op switch
        {
            ">=" => a >= b, ">" => a > b, "<=" => a <= b, "<" => a < b,
            "==" or "=" => a == b, "!=" => a != b, _ => true
        };

        private static int CompareVersions(string? a, string? b)
        {
            if (a == null || b == null) return -1;
            try { return new Version(a).CompareTo(new Version(b)); }
            catch { return string.Compare(a, b, StringComparison.OrdinalIgnoreCase); }
        }
    }

    public class InstallCondition
    {
        public ConditionType Type { get; set; }
        public string? Value { get; set; }
        public string? Value2 { get; set; }
        public string Operator { get; set; } = "==";
    }

    public enum ConditionType
    {
        AlwaysTrue, AlwaysFalse, OsVersion, Architecture,
        RegistryExists, RegistryValue, FileExists, DirectoryExists,
        CommandReturns, IsAdmin
    }
}
