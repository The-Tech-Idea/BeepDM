using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;


namespace TheTechIdea.Tools
{
    public class AssemblyItem : Entity
    {

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

        private string _assemblyname;
        public string Assemblyname
        {
            get { return _assemblyname; }
            set { SetProperty(ref _assemblyname, value); }
        }

        private string _typename;
        public string Typename
        {
            get { return _typename; }
            set { SetProperty(ref _typename, value); }
        }

        private List<AssemblyItemFieldDataTypes> _myfields = new List<AssemblyItemFieldDataTypes>();
        public List<AssemblyItemFieldDataTypes> MyFields
        {
            get { return _myfields; }
            set { SetProperty(ref _myfields, value); }
        } 
        public AssemblyItem()
        {

        }
    }
    public class AssemblyItemFieldDataTypes:Entity
    {

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


        private string _fieldname;
        public string fieldName
        {
            get { return _fieldname; }
            set { SetProperty(ref _fieldname, value); }
        }

        private string _fieldtype;
        public string fieldType
        {
            get { return _fieldtype; }
            set { SetProperty(ref _fieldtype, value); }
        }
        public AssemblyItemFieldDataTypes()
        {
        }


    }

}
