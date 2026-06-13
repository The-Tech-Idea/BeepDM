using System.Collections.Generic;
using System.Reflection;

namespace TheTechIdea.Beep.Editor.EntityDiscovery
{
    public interface IProjectScopeStrategy
    {
        IReadOnlyList<string> UserCodeAssemblyHints { get; }
        IReadOnlyList<string> FrameworkPrefixes { get; }
        bool LooksLikeUserCode(Assembly assembly);
        IReadOnlyList<Assembly> ResolveProjectScope();
        bool LooksLikeDataNamespace(string ns);
    }
}
