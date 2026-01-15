
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Environments.UserManagement
{
    public class Privilege: Entity
    {

        private int _privilegeid;
        public int PrivilegeID
        {
            get { return _privilegeid; }
            set { SetProperty(ref _privilegeid, value); }
        }

        private string _privilegename;
        public string PrivilegeName
        {
            get { return _privilegename; }
            set { SetProperty(ref _privilegename, value); }
        }

        private string _guidid;
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private string _componentguidid;
        public string ComponentGuidID
        {
            get { return _componentguidid; }
            set { SetProperty(ref _componentguidid, value); }
        }

        private string _componentname;
        public string ComponentName
        {
            get { return _componentname; }
            set { SetProperty(ref _componentname, value); }
        }

        private bool _isvisible;
        public bool IsVisible
        {
            get { return _isvisible; }
            set { SetProperty(ref _isvisible, value); }
        }

        private bool _islocked;
        public bool IsLocked
        {
            get { return _islocked; }
            set { SetProperty(ref _islocked, value); }
        }

        private bool _isenabled;
        public bool IsEnabled
        {
            get { return _isenabled; }
            set { SetProperty(ref _isenabled, value); }
        }

        private bool _isdisabled;
        public bool IsDisabled
        {
            get { return _isdisabled; }
            set { SetProperty(ref _isdisabled, value); }
        }

        private bool _canselect;
        public bool CanSelect
        {
            get { return _canselect; }
            set { SetProperty(ref _canselect, value); }
        }

        private bool _candelete;
        public bool CanDelete
        {
            get { return _candelete; }
            set { SetProperty(ref _candelete, value); }
        }

        private bool _canedit;
        public bool CanEdit
        {
            get { return _canedit; }
            set { SetProperty(ref _canedit, value); }
        }

        private bool _caninsert;
        public bool CanInsert
        {
            get { return _caninsert; }
            set { SetProperty(ref _caninsert, value); }
        }
    }
}
