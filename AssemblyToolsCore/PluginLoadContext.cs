﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;

namespace TheTechIdea.Tools.AssemblyHandling
{
    public class PluginLoadContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _resolver;

       
        public PluginLoadContext()
        {
           
            
        }
        public void SetResolver(string pluginPath)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);

        }
        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}
