using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Util
{
    [Serializable]
    public class ParentChildObject : Entity

    {

        private string _id;
        public string id
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

        private string _parentid;
        public string ParentID
        {
            get { return _parentid; }
            set { SetProperty(ref _parentid, value); }
        }

        private string _objtype;
        public string ObjType
        {
            get { return _objtype; }
            set { SetProperty(ref _objtype, value); }
        }

        private string _addinname;
        public string AddinName
        {
            get { return _addinname; }
            set { SetProperty(ref _addinname, value); }
        }


        private string _description;
        public string Description
        {
            get { return _description; }
            set { SetProperty(ref _description, value); }
        }

        private bool _mapped = false;
        public bool Mapped
        {
            get { return _mapped; }
            set { SetProperty(ref _mapped, value); }
        } 

        private bool _show = true;
        public bool Show
        {
            get { return _show; }
            set { SetProperty(ref _show, value); }
        } 

        private string _objectname;
        public string ObjectName
        {
            get { return _objectname; }
            set { SetProperty(ref _objectname, value); }
        }

        public ParentChildObject()
        {

        }
    }
}
