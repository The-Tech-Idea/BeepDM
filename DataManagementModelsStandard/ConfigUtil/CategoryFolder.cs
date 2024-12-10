
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
    public class CategoryFolder : Entity
    {

        private int _id;
        public int ID
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
        private string _parentguidid;
        public string ParentGuidID
        {
            get { return _parentguidid; }
            set { SetProperty(ref _parentguidid, value); }
        }
        private string _foldername;
        public string FolderName
        {
            get { return _foldername; }
            set { SetProperty(ref _foldername, value); }
        }

        private string _rootname;
        public string RootName
        {
            get { return _rootname; }
            set { SetProperty(ref _rootname, value); }
        }

        private string _parentname;
        public string ParentName
        {
            get { return _parentname; }
            set { SetProperty(ref _parentname, value); }
        }

        private int _parentid;
        public int ParentID
        {
            get { return _parentid; }
            set { SetProperty(ref _parentid, value); }
        }

        private bool _isparentroot;
        public bool IsParentRoot
        {
            get { return _isparentroot; }
            set { SetProperty(ref _isparentroot, value); }
        }
        private bool _isparentFolder;
        public bool IsParentFolder
        {
            get { return _isparentFolder; }
            set { SetProperty(ref _isparentFolder, value); }
        }

        private bool _isphysicalfolder;
        public bool IsPhysicalFolder
        {
            get { return _isphysicalfolder; }
            set { SetProperty(ref _isphysicalfolder, value); }
        }
        private BindingList<string> _items;
        public BindingList<string> items
        {
            get { return _items; }
            set { SetProperty(ref _items, value); }
        }

        public CategoryFolder()
        {
            _items = new BindingList<string>();
            GuidID = Guid.NewGuid().ToString();
        }
    }
}
