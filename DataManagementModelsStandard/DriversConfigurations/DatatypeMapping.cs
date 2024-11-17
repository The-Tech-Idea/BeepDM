using System;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.DriversConfigurations
{
    public class DatatypeMapping : Entity , IDatatypeMapping
    {
        public DatatypeMapping()
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

    private string _datatype;
    public string DataType
    {
        get { return _datatype; }
        set { SetProperty(ref _datatype, value); }
    }

        private string _dataSourceName;
        public string DataSourceName
        {
            get { return _dataSourceName; }
            set { SetProperty(ref _dataSourceName, value); }
        }

        private string _netDataType;
        public string NetDataType
        {
            get { return _netDataType; }
            set { SetProperty(ref _netDataType, value); }
        }

        private bool _fav;
        public bool Fav
        {
            get { return _fav; }
            set { SetProperty(ref _fav, value); }
        }
       
    
   


}

}
