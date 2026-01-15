
using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.DataBase
{
    public class EntityStructure : Entity, IEntityStructure,ICloneable
    {

        
private int _id;
    public int Id
    {
        get { return _id; }
        set { SetProperty(ref _id, value); }
    }
        private string _path;
        public string EntityPath
        {
            get { return _path; }
            set { SetProperty(ref _path, value); }
        }
        private string _entityname;
    public string EntityName
    {
        get { return _entityname; }
        set { SetProperty(ref _entityname, value); }
    }

    private string _originalentityname;
    public string OriginalEntityName
    {
        get { return _originalentityname; }
        set { SetProperty(ref _originalentityname, value); }
    }
        private string _sourceDataSourceID;
        public string SourceDataSourceID
        {
            get { return _sourceDataSourceID; }
            set { SetProperty(ref _sourceDataSourceID, value); }
        }
      
       
        private string _datasourceentityname;
    public string DatasourceEntityName
    {
        get { return _datasourceentityname; }
        set { SetProperty(ref _datasourceentityname, value); }
    }

    private string _caption;
    public string Caption
    {
        get { return _caption; }
        set { SetProperty(ref _caption, value); }
    }

        private string _DescriptionValue;
        public string Description
        {
            get { return _DescriptionValue; }
            set { SetProperty(ref _DescriptionValue, value); }
        }
        private DataSourceType _databasetype;
    public DataSourceType DatabaseType
    {
        get { return _databasetype; }
        set { SetProperty(ref _databasetype, value); }
    }

    private string _statusdescription;
    public string StatusDescription
    {
        get { return _statusdescription; }
        set { SetProperty(ref _statusdescription, value); }
    }

    private string _datasourceid;
    public string DataSourceID
    {
        get { return _datasourceid; }
        set { SetProperty(ref _datasourceid, value); }
    }

    //---------------- View Entity Properties ---------------

    private string _custombuildquery;
    public string CustomBuildQuery
    {
        get { return _custombuildquery; }
        set { SetProperty(ref _custombuildquery, value); }
    }

    private int _parentid;
    public int ParentId
    {
        get { return _parentid; }
        set { SetProperty(ref _parentid, value); }
    }

    private bool _show;
    public bool Show
    {
        get { return _show; }
        set { SetProperty(ref _show, value); }
    }

    private ViewType _viewtype;
    public ViewType Viewtype
    {
        get { return _viewtype; }
        set { SetProperty(ref _viewtype, value); }
    }

    private bool _editable;
    public bool Editable
    {
        get { return _editable; }
        set { SetProperty(ref _editable, value); }
    }

    private bool _drawn;
    public bool Drawn
    {
        get { return _drawn; }
        set { SetProperty(ref _drawn, value); }
    }

    private int _viewid;
    public int ViewID
    {
        get { return _viewid; }
        set { SetProperty(ref _viewid, value); }
    }
    //---------------- View Entity Properties ---------------

    private string _schemaorownerordatabase;
    public string SchemaOrOwnerOrDatabase
    {
        get { return _schemaorownerordatabase; }
        set { SetProperty(ref _schemaorownerordatabase, value); }
    }

    private EntityType _entityType= EntityType.Entity;
    public EntityType EntityType
    {
        get { return _entityType; }
        set { SetProperty(ref _entityType, value); }
    }

    private string _keytoken;
    public string KeyToken
    {
        get { return _keytoken; }
        set { SetProperty(ref _keytoken, value); }
    }

    private string _category;
    public string Category
    {
        get { return _category; }
        set { SetProperty(ref _category, value); }
    }

    private string _defaultcharttype;
    public string DefaultChartType
    {
        get { return _defaultcharttype; }
        set { SetProperty(ref _defaultcharttype, value); }
    }
        private List<EntityField> _fields;
        public List<EntityField> Fields
        {
            get { return _fields; }
            set { SetProperty(ref _fields, value); }
        }
        private List<EntityParameters> _parameters;
        public List<EntityParameters> Parameters
        {
            get { return _parameters; }
            set { SetProperty(ref _parameters, value); }
        }

        private List<RelationShipKeys> _relations;
        public List<RelationShipKeys> Relations
        {
            get { return _relations; }
            set { SetProperty(ref _relations, value); }
        }
        private string _primarykeystring;
        public string PrimaryKeyString
        {
            get { return _primarykeystring; }
            set { SetProperty(ref _primarykeystring, value); }
        }
        private List<EntityField> _primarykeys;
        public List<EntityField> PrimaryKeys
        {
            get { return _primarykeys; }
            set { SetProperty(ref _primarykeys, value); }
        }
        private List<AppFilter> _filters;
        public List<AppFilter> Filters
        {
            get { return _filters; }
            set { SetProperty(ref _filters, value); }
        }

        private int _startrow;
    public int StartRow
    {
        get { return _startrow; }
        set { SetProperty(ref _startrow, value); }
    }
    public int EndRow { get; set; }

    private string _guidid;
    public string GuidID
    {
        get { return _guidid; }
        set { SetProperty(ref _guidid, value); }
    }
        private bool _isLoaded = false;
        public bool IsLoaded
        {
            get { return _isLoaded; }
            set { SetProperty(ref _isLoaded, value); }
        }


        private bool _isSaved = false;
        public bool IsSaved
        {
            get { return _isSaved; }
            set { SetProperty(ref _isSaved, value); }
        }


        private bool _isSynced = false;
        public bool IsSynced
        {
            get { return _isSynced; }
            set { SetProperty(ref _isSynced, value); }
        }


        private bool _isCreated = false;
        public bool IsCreated
        {
            get { return _isCreated; }
            set { SetProperty(ref _isCreated, value); }
        }

        private bool _IsIdentity = false;
        public bool IsIdentity
        {
            get { return _IsIdentity; }
            set { SetProperty(ref _IsIdentity, value); }
        }
        public EntityStructure()
    {
        init();

    }
    public EntityStructure(string entityname)
    {
        init();
        EntityName = entityname;
    }
    public EntityStructure(string name, int parentId, string databaseID)
    {
        init();
        EntityName = name;
        ParentId = parentId;
        DataSourceID = databaseID;


    }
    public EntityStructure(string name, int parentId, string databaseID, ViewType viewtype)
    {
        init();
        EntityName = name;
        ParentId = parentId;
        DataSourceID = databaseID;
        Viewtype = viewtype;

    }
    private void init()
    {
        GuidID = Guid.NewGuid().ToString();
        StartRow = 0;
        EndRow = 0;
        Fields = new List<EntityField>();
        Parameters = new List<EntityParameters>();
        Relations = new List<RelationShipKeys>();
        PrimaryKeys = new List<EntityField>();
        Filters = new List<AppFilter>();
    }

    public object Clone()
    {
        return this.MemberwiseClone();
    }
}

    public class EntityField : Entity, IEntityField
    {


        private int _id;
        public int id
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

        private string _fieldname;
        public string FieldName
        {
            get { return _fieldname; }
            set { SetProperty(ref _fieldname, value); }
        }

        private string _DescriptionValue;
        public string Description
        {
            get { return _DescriptionValue; }
            set { SetProperty(ref _DescriptionValue, value); }
        }
        private string _originalfieldname;
        public string Originalfieldname
        {
            get { return _originalfieldname; }
            set { SetProperty(ref _originalfieldname, value); }
        }

        private string _fieldtype;
        public string Fieldtype
        {
            get { return _fieldtype; }
            set { SetProperty(ref _fieldtype, value); }
        }
        private int _size;
        public int Size
        {
            get { return _size==0 && _size1>0 ? Size1: _size; }
            set { SetProperty(ref _size, value); }
        }
        private int _size1;
        public int Size1
        {
            get { return _size1; }
            set { SetProperty(ref _size1, value); }
        }

        private int _size2;
        public int Size2
        {
            get { return _size2; }
            set { SetProperty(ref _size2, value); }
        }

        private short _numericprecision;
        public short NumericPrecision
        {
            get { return _numericprecision; }
            set { SetProperty(ref _numericprecision, value); }
        }

        private short _numericscale;
        public short NumericScale
        {
            get { return _numericscale; }
            set { SetProperty(ref _numericscale, value); }
        }

        private DbFieldCategory _FieldCategory;
        public DbFieldCategory FieldCategory
        {
            get { return _FieldCategory; }
            set { SetProperty(ref _FieldCategory, value); }
        }
        //IsRequired ValueMin ValueMax IsIndexed

        private int _valuemin;
        public int ValueMin
        {
            get { return _valuemin; }
            set { SetProperty(ref _valuemin, value); }
        }

        private int _valuemax;
        public int ValueMax
        {
            get { return _valuemax; }
            set { SetProperty(ref _valuemax, value); }
        }


        private bool _isrequired;
        public bool IsRequired
        {
            get { return _isrequired; }
            set { SetProperty(ref _isrequired, value); }
        }

        private bool _isindexed;
        public bool IsIndexed
        {
            get { return _isindexed; }
            set { SetProperty(ref _isindexed, value); }
        }

        private bool _isautoincrement;
        public bool IsAutoIncrement
        {
            get { return _isautoincrement; }
            set { SetProperty(ref _isautoincrement, value); }
        }

        private bool _allowdbnull;
        public bool AllowDBNull
        {
            get { return _allowdbnull; }
            set { SetProperty(ref _allowdbnull, value); }
        }

        private bool _ischeck;
        public bool IsCheck
        {
            get { return _ischeck; }
            set { SetProperty(ref _ischeck, value); }
        }

        private bool _isunique;
        public bool IsUnique
        {
            get { return _isunique; }
            set { SetProperty(ref _isunique, value); }
        }

        private bool _iskey;
        public bool IsKey
        {
            get { return _iskey; }
            set { SetProperty(ref _iskey, value); }
        }

        private bool _checked;
        public bool Checked
        {
            get { return _checked; }
            set { SetProperty(ref _checked, value); }
        }

        private int _fieldindex;
        public int FieldIndex
        {
            get { return _fieldindex; }
            set { SetProperty(ref _fieldindex, value); }
        }

        private bool _valueretrievedfromparent;
        public bool ValueRetrievedFromParent
        {
            get { return _valueretrievedfromparent; }
            set { SetProperty(ref _valueretrievedfromparent, value); }
        }
        
        private bool _DisplayField = false;
        public bool IsDisplayField
        {
            get { return _DisplayField; }
            set { SetProperty(ref _DisplayField, value); }
        }
        private bool _IsIdentity = false;
        public bool IsIdentity
        {
            get { return _IsIdentity; }
            set { SetProperty(ref _IsIdentity, value); }
        }

        private string _entityname;
        public string EntityName
        {
            get { return _entityname; }
            set { SetProperty(ref _entityname, value); }
        }
        private string _caption;
        public string Caption
        {
            get { return _caption; }
            set { SetProperty(ref _caption, value); }
        }
        // New fields
        public int OrdinalPosition { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsRowVersion { get; set; }
        public bool IsLong { get; set; }
        public string DefaultValue { get; set; }
        public string Expression { get; set; }
        public string BaseTableName { get; set; }
        public string BaseColumnName { get; set; }
        public int MaxLength { get; set; }
        public bool IsFixedLength { get; set; }
        public bool IsHidden { get; set; }
        public EntityField()
        {
            Fieldtype = "Text";
           FieldCategory = DbFieldCategory.String;
            GuidID = Guid.NewGuid().ToString();
            IsAutoIncrement = false;
            AllowDBNull = true;
            IsCheck = false;
            IsUnique = false;
            IsKey = false;
            Checked = false;
            IsDisplayField = false;

        }
        public EntityField Clone()
        {
            return this.Clone();
        }
    }

    public class EntityParameters : Entity
    {


        private string _parametername;
        public string parameterName
        {
            get { return _parametername; }
            set { SetProperty(ref _parametername, value); }
        }

        private string _parametertype;
        public string parametertype
        {
            get { return _parametertype; }
            set { SetProperty(ref _parametertype, value); }
        }

        private DbFieldCategory _parametercategory;
        public DbFieldCategory parameterCategory
        {
            get { return _parametercategory; }
            set { SetProperty(ref _parametercategory, value); }
        }

        private Boolean _iskey;
        public Boolean IsKey
        {
            get { return _iskey; }
            set { SetProperty(ref _iskey, value); }
        } 

        private int _parameterindex;
        public int parameterIndex
        {
            get { return _parameterindex; }
            set { SetProperty(ref _parameterindex, value); }
        }

        private string _stringvalue;
        public string StringValue
        {
            get { return _stringvalue; }
            set { SetProperty(ref _stringvalue, value); }
        }

        private DateTime _datetimevalue;
        public DateTime DateTimeValue
        {
            get { return _datetimevalue; }
            set { SetProperty(ref _datetimevalue, value); }
        }

        private int _intvalue;
        public int intValue
        {
            get { return _intvalue; }
            set { SetProperty(ref _intvalue, value); }
        }

        private string _sourceentityname;
        public string SourceEntityName
        {
            get { return _sourceentityname; }
            set { SetProperty(ref _sourceentityname, value); }
        }

        private string _sourcefieldname;
        public string SourceFieldName
        {
            get { return _sourcefieldname; }
            set { SetProperty(ref _sourcefieldname, value); }
        }

        private string _sourcedatasource;
        public string SourceDataSource
        {
            get { return _sourcedatasource; }
            set { SetProperty(ref _sourcedatasource, value); }
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

     

      


        public EntityParameters()
        {
            GuidID = Guid.NewGuid().ToString();
            parametertype = "Text"; //string or numeric or date gets filled later.by pick list 
            parameterCategory = DbFieldCategory.String;

        }
    }

   

    public class ColumnLookupList : Entity, IColumnLookupList
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

        private object _value;
        public object Value
        {
            get { return _value; }
            set { SetProperty(ref _value, value); }
        }

        private string _display;
        public string Display
        {
            get { return _display; }
            set { SetProperty(ref _display, value); }
        }

        public ColumnLookupList()
        {
            GuidID = Guid.NewGuid().ToString();
        }
    }

    public class InsertTransaction : IInsertTransaction
    {
        public InsertTransaction()
        {
            GuidID = Guid.NewGuid().ToString();
        }
        public int ID { get; set; }
        public string GuidID { get; set; }
        public string RecordIndex { get; set; }
        public string SourceField { get; set; }
        public string DestField { get; set; }
        public string SourceValue { get; set; }
        public Type FieldType { get; set; }
    }

}
