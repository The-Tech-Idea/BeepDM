using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.DataBase
{
    public class QueryStructure : Entity, IQueryStructure
    
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
    public QueryStructure()
    {

    }

    private string _queryname;
    public string QueryName
    {
        get { return _queryname; }
        set { SetProperty(ref _queryname, value); }
    }

    private string _querydescription;
    public string QueryDescription
    {
        get { return _querydescription; }
        set { SetProperty(ref _querydescription, value); }
    }

    private string _querystring;
    public string Querystring
    {
        get { return _querystring; }
        set { SetProperty(ref _querystring, value); }
    }

    private List<EntityField> _selectedcolumns;
    public List<EntityField> SelectedColumns
    {
        get { return _selectedcolumns; }
        set { SetProperty(ref _selectedcolumns, value); }
    }

    private List<EntityStructure> _fromentities;
    public List<EntityStructure> FromEntities
    {
        get { return _fromentities; }
        set { SetProperty(ref _fromentities, value); }
    }

    private List<QueryFieldsandValues> _fieldsandvalues;
    public List<QueryFieldsandValues> FieldsandValues
    {
        get { return _fieldsandvalues; }
        set { SetProperty(ref _fieldsandvalues, value); }
    }

    private int _parentviewid;
    public int ParentViewID
    {
        get { return _parentviewid; }
        set { SetProperty(ref _parentviewid, value); }
    }

    private int _parenttableid;
    public int ParentTableID
    {
        get { return _parenttableid; }
        set { SetProperty(ref _parenttableid, value); }
    }



}

public class QueryFieldsandValues:Entity
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
    public QueryFieldsandValues()
    {

    }

    private EntityField _comparefield1;
    public EntityField CompareField1
    {
        get { return _comparefield1; }
        set { SetProperty(ref _comparefield1, value); }
    }

    private string _fieldvalue;
    public string Fieldvalue
    {
        get { return _fieldvalue; }
        set { SetProperty(ref _fieldvalue, value); }
    }

    private string _comparison;
    public string Comparison
    {
        get { return _comparison; }
        set { SetProperty(ref _comparison, value); }
    }

}

public class FkListforSQLlite
    {
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public FkListforSQLlite()
        {

        }
        public int id { get; set; } // Integer Foreign key ID number
        public int seq { get; set; }// Integer Column sequence number for this key
        public string table { get; set; }//   Text Name of foreign table
        public string from { get; set; } //  Text Local column name
        public string to { get; set; }// Text    Foreign column name
        public string on_update { get; set; } // Text ON UPDATE action
        public string on_delete { get; set; } // Text    ON DELETE action
        public string match { get; set; } // Text Always NONE
    }
}
