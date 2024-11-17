using System;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.ConfigUtil
{
    public class DefaultValue : Entity
    {
        public DefaultValue()
        {
            GuidID = Guid.NewGuid().ToString();
        }

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

        private string _propertyname;
        public string propertyName
        {
            get { return _propertyname; }
            set { SetProperty(ref _propertyname, value); }
        }

        private string _propoertvalue;
        public string propoertValue
        {
            get { return _propoertvalue; }
            set { SetProperty(ref _propoertvalue, value); }
        }

        private string _rule;
        public string Rule
        {
            get { return _rule; }
            set { SetProperty(ref _rule, value); }
        }

        private DefaultValueType _propertytype;
        public DefaultValueType propertyType
        {
            get { return _propertytype; }
            set { SetProperty(ref _propertytype, value); }
        }

        private string _propertycategory;
        public string propertyCategory
        {
            get { return _propertycategory; }
            set { SetProperty(ref _propertycategory, value); }
        }

    }

}
