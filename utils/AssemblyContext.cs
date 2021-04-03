
using System.Reflection;
using System.Runtime.Loader;

namespace TheTechIdea.Tools
{
    public class AssemblyContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _resolver;

        public AssemblyContext(string mainAssemblyToLoadPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(mainAssemblyToLoadPath);
        }

        protected override Assembly Load(AssemblyName name)
        {
            string assemblyPath = _resolver.ResolveAssemblyToPath(name);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }
    }
}
