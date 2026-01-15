
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Environments
{
    public class Product :Entity, IProduct
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

        private string _productid;
        public string ProductID
        {
            get { return _productid; }
            set { SetProperty(ref _productid, value); }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set { SetProperty(ref _description, value); }
        }

        private string _version;
        public string Version
        {
            get { return _version; }
            set { SetProperty(ref _version, value); }
        }
    }
}
