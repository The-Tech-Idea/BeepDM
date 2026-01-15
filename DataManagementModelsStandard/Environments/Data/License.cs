
using System;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Environments.UserManagement
{
    public class License : Entity
    {

        private string _guidid;
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        }

        private int _userlicenseid;
        public int UserLicenseID
        {
            get { return _userlicenseid; }
            set { SetProperty(ref _userlicenseid, value); }
        }

        private string _licenceid;
        public string LicenceID
        {
            get { return _licenceid; }
            set { SetProperty(ref _licenceid, value); }
        }

        private string _product;
        public string Product
        {
            get { return _product; }
            set { SetProperty(ref _product, value); }
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

        private bool _autorenewal;
        public bool AutoRenewal
        {
            get { return _autorenewal; }
            set { SetProperty(ref _autorenewal, value); }
        }

    }
}
