
using System;
using System.Collections.Generic;
using TheTechIdea.Util;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.DataBase
{
    public class EntityStructure : IEntityStructure,ICloneable
    {

        public int Id { get; set; }
        private string m_entname;
        private string m_OriginalEntityName;
        public string EntityName { get { return m_entname; } set { m_OriginalEntityName = m_entname; m_entname = value;  } }
        public string OriginalEntityName { get { return m_OriginalEntityName; } set { m_OriginalEntityName = value; } }
        public string DatasourceEntityName { get; set; }
        public string Caption { get; set; }
        public DataSourceType DatabaseType { get; set; }
        public string StatusDescription { get; set; }
        public string DataSourceID { get; set; }

        //---------------- View Entity Properties ---------------
        public string CustomBuildQuery { get; set; }
        public int ParentId { get; set; }
        public bool Show { get; set; } = true;
        public ViewType Viewtype { get; set; }
        public bool Editable { get; set; }
        public bool Drawn { get; set; } = false;
        public int ViewID { get; set; }
        //---------------- View Entity Properties ---------------
        public string SchemaOrOwnerOrDatabase { get; set; }
        public Boolean Created { get; set; } = true;
        public string KeyToken { get; set; }
        public string Category { get; set; }
        public List<EntityField> Fields { get; set; } = new List<EntityField>();
        public List<EntityParameters> Paramenters { get; set; } = new List<EntityParameters>();
        public List<RelationShipKeys> Relations { get; set; } = new List<RelationShipKeys>();
        public List<EntityField> PrimaryKeys { get; set; } = new List<EntityField>();
        public List<ReportFilter> Filters { get; set; }
        public EntityStructure()
        {


        }
        public EntityStructure(string entityname)
        {
            EntityName = entityname;
        }
        public EntityStructure(string name, int parentId, string databaseID)
        {

            EntityName = name;
            ParentId = parentId;
            DataSourceID = databaseID;


        }
        public EntityStructure(string name, int parentId, string databaseID, ViewType viewtype)
        {

            EntityName = name;
            ParentId = parentId;
            DataSourceID = databaseID;
            Viewtype = viewtype;

        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
    public class EntityField :IEntityField
    {

        public int id { get; set; }
        private string p_fieldname;
        public string fieldname { get { return p_fieldname; } set { p_oribinalfieldname = p_fieldname;p_fieldname=value ; } }
        private string p_oribinalfieldname;
        public string Originalfieldname { get { return p_oribinalfieldname; } set { p_oribinalfieldname = value; } }
        public string fieldtype { get; set; } = "Text"; //string or numeric or date gets filled later.by pick list 
        public int Size1 { get; set; }
        public int Size2 { get; set; }
        public short NumericPrecision { get; set; }
        public short NumericScale { get; set; }
        public DbFieldCategory fieldCategory { get; set; } = DbFieldCategory.String;
        public bool IsAutoIncrement { get; set; } = false;
        public bool AllowDBNull { get; set; } = false;
        public bool IsCheck { get; set; } = false;
        public bool IsUnique { get; set; } = false;
        public bool IsKey { get; set; } = false;
        public bool Checked { get; set; } = false;
        public int FieldIndex { get; set; }
        public bool ValueRetrievedFromParent { get; set; }
     //   public string statusdescription { get; set; }
      //  public Boolean created { get; set; }
        public string EntityName { get; set; }
       

        public EntityField()
        {


        }
    }
    public class EntityParameters 
    {

        public int id { get; set; }
        public string parameterName { get; set; }
        public string parametertype { get; set; } = "Text"; //string or numeric or date gets filled later.by pick list 
        public DbFieldCategory parameterCategory { get; set; } = DbFieldCategory.String;
        public Boolean IsKey { get; set; } = false;
        public int parameterIndex { get; set; }
        public string StringValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public int intValue { get; set; }
        public string SourceEntityName { get; set; }
        public string SourceFieldName { get; set; }
        public string SourceDataSource { get; set; }


        public EntityParameters()
        {


        }
    }
    public class RelationShipKeys :IRelationShipKeys
    {
        public string RalationName { get; set; }
        public string RelatedEntityID { get; set; }
        public string RelatedEntityColumnID { get; set; }
        public int RelatedColumnSequenceID { get; set; }
        public string EntityColumnID { get; set; }
        public int EntityColumnSequenceID { get; set; }

        public RelationShipKeys()
        {

        }
        public RelationShipKeys(string pParentEntityID, string pParentEntityColumnID, string pEntityColumnID)
        {
            RelatedEntityID = pParentEntityID;
            RelatedEntityColumnID = pParentEntityColumnID;
            EntityColumnID = pEntityColumnID;

        }
    }
    public class ColumnLookupList :IColumnLookupList
    {
        public object Value { get; set; }
        public string Display { get; set; }

        public ColumnLookupList()
        {

        }
    }
    public class InsertTransaction : IInsertTransaction
    {
        public InsertTransaction()
        {

        }
        public string RecordIndex { get; set; }
        public string SourceField { get; set; }
        public string DestField { get; set; }
        public string SourceValue { get; set; }
        public Type FieldType { get; set; }
    }

}
