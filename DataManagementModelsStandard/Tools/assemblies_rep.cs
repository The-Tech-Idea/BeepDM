using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TheTechIdea.Util;

namespace TheTechIdea.Tools
{
    public class assemblies_rep
    {
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public Assembly DllLib { get; set; }
        public string DllLibPath { get; set; }
        public string DllName { get; set; }
        public FolderFileTypes FileTypes { get; set; }
        public assemblies_rep(Assembly pDllLib, string pDllLibPath, string pDllName,FolderFileTypes fileTypes)
        {
            DllLib = pDllLib;
            DllLibPath = pDllLibPath;
            DllName = pDllName;
            FileTypes = fileTypes;
        }
    }
}
