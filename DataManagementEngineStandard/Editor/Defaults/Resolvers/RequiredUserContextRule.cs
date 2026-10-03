using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor.Importing;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    internal static class RequiredUserContextRule
    {
        private static void Deny() => throw new ImportTransformationException(ImportTransformationStage.Defaults);
        private static void Check()
        {
            RequiredDefaultResolution.Current?.Token.ThrowIfCancellationRequested();
            if (RequiredDefaultResolution.Current?.Failed == true) Deny();
        }

        internal static string Resolve(string rule, IPassedArgs parameters)
        {
            Check();
            var open = rule.IndexOf('(');
            var op = (open < 0 ? rule : rule.Substring(0, open)).Trim().ToUpperInvariant();
            var arg = open < 0 ? null : RequiredBuiltInRule.RequireAtom(
                RequiredBuiltInRule.SplitArguments(rule.Substring(open + 1, rule.Length - open - 2))[0], nonempty: true);
            var result = op switch
            {
                "USERNAME" or "CURRENTUSER" or "USERLOGIN" => Environment.UserName,
                "USERDOMAIN" => Environment.UserDomainName,
                "USEREMAIL" => ContextText(parameters, "UserEmail"),
                "USERROLE" => ContextText(parameters, arg == null ? "UserRole" : arg + "Role"),
                "USERPROFILE" => Profile(arg),
                "USERID" or "USERPRINCIPAL" or "USERGROUP" => WindowsValue(op),
                _ => throw new ImportTransformationException(ImportTransformationStage.Defaults)
            };
            Check();
            return Text(result);
        }

        private static string Text(object value)
        {
            Check();
            if (value is not string text || string.IsNullOrWhiteSpace(text) || text.Length > 4096 || text.Any(char.IsControl)) Deny();
            return (string)value;
        }

        private static string ContextText(IPassedArgs parameters, string key)
        {
            Check();
            if (parameters == null) Deny();
            var properties = parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
            var objects = parameters.Objects;
            Check();
            if (objects?.Count > 10000) Deny();
            var named = objects?.Where(item => item != null && string.Equals(item.Name, key, StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
            if (properties.Length + (named?.Length ?? 0) != 1) Deny();
            if (properties.Length == 1)
            {
                var property = properties[0];
                if (property.GetMethod?.IsPublic != true || property.GetIndexParameters().Length != 0) Deny();
                var value = property.GetValue(parameters);
                Check();
                return Text(value);
            }
            return Text(named[0].obj);
        }

        private static string WindowsValue(string op)
        {
            Check();
            if (!OperatingSystem.IsWindows()) throw new ImportTransformationException(ImportTransformationStage.Defaults);
            using var identity = WindowsIdentity.GetCurrent();
            Check();
            if (identity == null) Deny();
            if (op == "USERID") return Text(identity.User?.Value);
            if (op == "USERPRINCIPAL") return Text(identity.Name);
            var principal = new WindowsPrincipal(identity);
            foreach (var group in new[] { WindowsBuiltInRole.Administrator, WindowsBuiltInRole.PowerUser, WindowsBuiltInRole.User })
            {
                Check();
                var member = principal.IsInRole(group);
                Check();
                if (member) return group switch { WindowsBuiltInRole.Administrator => "Administrators", WindowsBuiltInRole.PowerUser => "Power Users", _ => "Users" };
            }
            Deny();
            return null;
        }

        private static string Profile(string folder)
        {
            Check();
            if (folder == null) return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (folder.Equals("TEMP", StringComparison.OrdinalIgnoreCase)) return Path.GetTempPath();
            if (folder.Equals("DOWNLOADS", StringComparison.OrdinalIgnoreCase)) return Downloads();
            var special = folder.ToUpperInvariant() switch
            {
                "DOCUMENTS" or "MYDOCUMENTS" => Environment.SpecialFolder.MyDocuments,
                "DESKTOP" => Environment.SpecialFolder.Desktop,
                "PICTURES" or "MYPICTURES" => Environment.SpecialFolder.MyPictures,
                "MUSIC" or "MYMUSIC" => Environment.SpecialFolder.MyMusic,
                "VIDEOS" or "MYVIDEOS" => Environment.SpecialFolder.MyVideos,
                "APPDATA" => Environment.SpecialFolder.ApplicationData,
                "LOCALAPPDATA" => Environment.SpecialFolder.LocalApplicationData,
                _ => throw new ImportTransformationException(ImportTransformationStage.Defaults)
            };
            return Environment.GetFolderPath(special);
        }

        private static string Downloads()
        {
            Check();
            if (!OperatingSystem.IsWindows()) throw new ImportTransformationException(ImportTransformationStage.Defaults);
            // Balance only our successful COM initialization and always free shell task memory.
            var initialized = CoInitializeEx(IntPtr.Zero, 0);
            if (initialized < 0 && initialized != unchecked((int)0x80010106)) Deny();
            var path = IntPtr.Zero;
            try
            {
                Check();
                var id = new Guid("374DE290-123F-4565-9164-39C4925E467B");
                var result = SHGetKnownFolderPath(ref id, 0, IntPtr.Zero, out path);
                Check();
                if (result < 0 || path == IntPtr.Zero) Deny();
                return Text(Marshal.PtrToStringUni(path));
            }
            finally
            {
                if (path != IntPtr.Zero) Marshal.FreeCoTaskMem(path);
                if (initialized >= 0) CoUninitialize();
            }
        }

        [DllImport("ole32.dll", ExactSpelling = true)]
        private static extern int CoInitializeEx(IntPtr reserved, uint mode);
        [DllImport("ole32.dll", ExactSpelling = true)]
        private static extern void CoUninitialize();
        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern int SHGetKnownFolderPath(ref Guid folder, uint flags, IntPtr token, out IntPtr path);
    }
}
