
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.ConfigUtil
{
    public class StorageFolders : Entity
    {

        private string _id;
        public string ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _guidid;
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        }

        private string _folderpath;
        public string FolderPath
        {
            get { return _folderpath; }
            set { SetProperty(ref _folderpath, value); }
        }

        private FolderFileTypes _folderfilestype;
        public FolderFileTypes FolderFilesType
        {
            get { return _folderfilestype; }
            set { SetProperty(ref _folderfilestype, value); }
        }

        private string _entrypointclass;
        public string EntrypointClass
        {
            get { return _entrypointclass; }
            set { SetProperty(ref _entrypointclass, value); }
        }
        public StorageFolders()
        {
            GuidID = Guid.NewGuid().ToString();
        }
        public StorageFolders(string pFolderPath)
        {
            GuidID = Guid.NewGuid().ToString();
            FolderPath = pFolderPath;
        }
        public StorageFolders(string pFolderPath, FolderFileTypes pFoderType)
        {
            GuidID = Guid.NewGuid().ToString();
            FolderPath = pFolderPath;
            FolderFilesType = pFoderType;
        }
    }
}
