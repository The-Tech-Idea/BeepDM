using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.NOSQL
{
    public class DatabaseCollection : Entity
    {
        public DatabaseCollection()
        { }

        private int _id;
        public int ID
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

        private int _countofdocuments;
        public int CountOfDocuments
        {
            get { return _countofdocuments; }
            set { SetProperty(ref _countofdocuments, value); }
        }

        private string _databasname;
        public string DatabasName
        {
            get { return _databasname; }
            set { SetProperty(ref _databasname, value); }
        }

        private List<string> _collections = new List<string>();
        public List<string> Collections
        {
            get { return _collections; }
            set { SetProperty(ref _collections, value); }
        }
    }
}
