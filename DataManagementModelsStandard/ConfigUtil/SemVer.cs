using System;

namespace TheTechIdea.Beep.ConfigUtil
{
    /// <summary>
    /// Minimal semantic-version parse/compare shared by the version-tracking and migrate-on-startup
    /// paths. Pre-release/build metadata is parsed off but ignored for ordering (Major.Minor.Patch
    /// only) — sufficient for the gate's "declared &gt; recorded" decision.
    /// </summary>
    public static class SemVer
    {
        /// <summary>Parses "M.m.p", "M.m", "M", optionally with a "-prerelease" or "+build" suffix.</summary>
        public static bool TryParse(string version, out int major, out int minor, out int patch)
        {
            major = minor = patch = 0;
            if (string.IsNullOrWhiteSpace(version)) return false;

            var core = version.Trim();
            int cut = core.IndexOfAny(new[] { '-', '+' });
            if (cut >= 0) core = core.Substring(0, cut);

            var parts = core.Split('.');
            if (parts.Length == 0) return false;

            if (!int.TryParse(parts[0], out major)) return false;
            if (parts.Length > 1 && !int.TryParse(parts[1], out minor)) return false;
            if (parts.Length > 2 && !int.TryParse(parts[2], out patch)) return false;
            return true;
        }

        /// <summary>-1 / 0 / +1 comparing a against b by Major.Minor.Patch. Unparsable sides sort as 0.0.0.</summary>
        public static int Compare(string a, string b)
        {
            TryParse(a, out var aMaj, out var aMin, out var aPat);
            TryParse(b, out var bMaj, out var bMin, out var bPat);
            if (aMaj != bMaj) return aMaj.CompareTo(bMaj);
            if (aMin != bMin) return aMin.CompareTo(bMin);
            return aPat.CompareTo(bPat);
        }

        /// <summary>Compares two <see cref="DatabaseVersion"/> by their numeric components.</summary>
        public static int Compare(DatabaseVersion a, DatabaseVersion b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            if (a.Major != b.Major) return a.Major.CompareTo(b.Major);
            if (a.Minor != b.Minor) return a.Minor.CompareTo(b.Minor);
            return a.Patch.CompareTo(b.Patch);
        }
    }
}
