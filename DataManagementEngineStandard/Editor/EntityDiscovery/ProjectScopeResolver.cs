using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TheTechIdea.Beep.Editor.EntityDiscovery
{
    public enum DiscoveryScope
    {
        Project = 0,
        AllLoaded = 1,
        Explicit = 2
    }

    public sealed class ProjectScopeResolver : IProjectScopeStrategy
    {
        public static IProjectScopeStrategy Default { get; } = new ProjectScopeResolver();

        public IReadOnlyList<string> UserCodeAssemblyHints { get; }
        public IReadOnlyList<string> FrameworkPrefixes { get; }

        public ProjectScopeResolver(
            IReadOnlyList<string>? userCodeHints = null,
            IReadOnlyList<string>? frameworkPrefixes = null)
        {
            UserCodeAssemblyHints = userCodeHints ?? _defaultUserCodeHints;
            FrameworkPrefixes = frameworkPrefixes ?? _defaultFrameworkPrefixes;
        }

        private static readonly string[] _defaultUserCodeHints =
        {
            ".Data",
            ".Entities",
            ".Entity",
            ".Models",
            ".Domain",
            ".Infrastructure",
            ".Persistence",
            ".DAL",
            ".Repositories",
            ".Schema"
        };

        private static readonly string[] _defaultFrameworkPrefixes =
        {
            "System.",
            "Microsoft.",
            "netstandard",
            "mscorlib",
            "MudBlazor",
            "BeepDM",
            "TheTechIdea"
        };

        public bool LooksLikeUserCode(Assembly assembly)
        {
            if (assembly == null || assembly.IsDynamic) return false;
            var name = assembly.GetName().Name ?? string.Empty;
            if (string.IsNullOrEmpty(name)) return false;
            if (FrameworkPrefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase))) return false;
            if (UserCodeAssemblyHints.Any(h => name.EndsWith(h, StringComparison.OrdinalIgnoreCase))) return true;
            return false;
        }

        public IReadOnlyList<Assembly> ResolveProjectScope()
        {
            var root = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            if (root == null || root.IsDynamic) return Array.Empty<Assembly>();

            var result = new List<Assembly> { root };

            AssemblyName[] refs;
            try { refs = root.GetReferencedAssemblies(); }
            catch { refs = Array.Empty<AssemblyName>(); }

            foreach (var refName in refs)
            {
                Assembly refAsm = null;
                try { refAsm = Assembly.Load(refName); } catch { continue; }
                if (refAsm == null || refAsm.IsDynamic) continue;

                var simpleName = refAsm.GetName().Name ?? string.Empty;
                if (string.IsNullOrEmpty(simpleName)) continue;

                if (IsFrameworkAssembly(simpleName)) continue;
                if (!IsUserCodeAssembly(simpleName)) continue;

                result.Add(refAsm);
            }

            return result;
        }

        private bool IsFrameworkAssembly(string simpleName) =>
            FrameworkPrefixes.Any(p => simpleName.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        private bool IsUserCodeAssembly(string simpleName) =>
            UserCodeAssemblyHints.Any(h => simpleName.EndsWith(h, StringComparison.OrdinalIgnoreCase));

        public bool LooksLikeDataNamespace(string ns)
        {
            if (string.IsNullOrWhiteSpace(ns)) return false;
            var segments = ns.Split('.');
            foreach (var seg in segments)
            {
                if (seg.Equals("Data",      StringComparison.OrdinalIgnoreCase)) return true;
                if (seg.Equals("Entities",  StringComparison.OrdinalIgnoreCase)) return true;
                if (seg.Equals("Entity",    StringComparison.OrdinalIgnoreCase)) return true;
                if (seg.Equals("Models",    StringComparison.OrdinalIgnoreCase)) return true;
                if (seg.Equals("Model",     StringComparison.OrdinalIgnoreCase)) return true;
                if (seg.Equals("Domain",    StringComparison.OrdinalIgnoreCase)) return true;
                if (seg.Equals("Persistence", StringComparison.OrdinalIgnoreCase)) return true;
                if (seg.StartsWith("EF",    StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}

