
using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Environments.UserManagement
{
    public class Group : Entity
    {

        private int _groupid;
        public int GroupID
        {
            get { return _groupid; }
            set { SetProperty(ref _groupid, value); }
        }

        private string _guidid;
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        }

        private string _groupname;
        public string GroupName
        {
            get { return _groupname; }
            set { SetProperty(ref _groupname, value); }
        }

        private List<string> _privileges;
        public List<string> Privileges
        {
            get { return _privileges; }
            set { SetProperty(ref _privileges, value); }
        }

        private List<string> _users;
        public List<string> Users
        {
            get { return _users; }
            set { SetProperty(ref _users, value); }
        }

        private DateTime _startdate;
        public DateTime StartDate
        {
            get { return _startdate; }
            set { SetProperty(ref _startdate, value); }
        }

        private DateTime _enddate;
        public DateTime EndDate
        {
            get { return _enddate; }
            set { SetProperty(ref _enddate, value); }
        }

        private bool _isadmin;
        public bool IsAdmin
        {
            get { return _isadmin; }
            set { SetProperty(ref _isadmin, value); }
        }

        private bool _isactive;
        public bool IsActive
        {
            get { return _isactive; }
            set { SetProperty(ref _isactive, value); }
        }
    }
}
