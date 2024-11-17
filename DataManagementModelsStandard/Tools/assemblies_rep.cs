using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools
{
    public class assemblies_rep : Entity
    {

        private int _id;
        public int ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _guidid = Guid.NewGuid().ToString();
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        } 

        private Assembly _dlllib;
        public Assembly DllLib
        {
            get { return _dlllib; }
            set { SetProperty(ref _dlllib, value); }
        }

        private string _dlllibpath;
        public string DllLibPath
        {
            get { return _dlllibpath; }
            set { SetProperty(ref _dlllibpath, value); }
        }

        private string _dllname;
        public string DllName
        {
            get { return _dllname; }
            set { SetProperty(ref _dllname, value); }
        }

        private FolderFileTypes _filetypes;
        public FolderFileTypes FileTypes
        {
            get { return _filetypes; }
            set { SetProperty(ref _filetypes, value); }
        }
        public assemblies_rep(Assembly pDllLib, string pDllLibPath, string pDllName, FolderFileTypes fileTypes)
        {
            DllLib = pDllLib;
            DllLibPath = pDllLibPath;
            DllName = pDllName;
            FileTypes = fileTypes;
        }
    }
}
