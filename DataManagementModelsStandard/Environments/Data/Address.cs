
using TheTechIdea.Beep.Environments;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Environments.UserManagement
{
    public class Address : Entity
    {

        private int _addressid;
        public int AddressID
        {
            get { return _addressid; }
            set { SetProperty(ref _addressid, value); }
        }

        private string _guidid;
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        }

        private string _address1;
        public string Address1
        {
            get { return _address1; }
            set { SetProperty(ref _address1, value); }
        }

        private string _address2;
        public string Address2
        {
            get { return _address2; }
            set { SetProperty(ref _address2, value); }
        }

        private string _city;
        public string City
        {
            get { return _city; }
            set { SetProperty(ref _city, value); }
        }

        private string _state;
        public string State
        {
            get { return _state; }
            set { SetProperty(ref _state, value); }
        }


        private string _country;
        public string Country
        {
            get { return _country; }
            set { SetProperty(ref _country, value); }
        }

        private string _zcode;
        public string Zcode
        {
            get { return _zcode; }
            set { SetProperty(ref _zcode, value); }
        }

        private AddressTypes _addresstype;
        public AddressTypes AddressType
        {
            get { return _addresstype; }
            set { SetProperty(ref _addresstype, value); }
        }


    }

}
