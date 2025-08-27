using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Report
{
    public interface IAppFilter
    {
         int ID { get; set; }
         string GuidID { get; set; }
        string FieldName { get; set; }
        object FilterValue { get; set; }
        string Operator { get; set; }
        string valueType { get; set; }
         string FilterValue1 { get; set; }
         Type FieldType { get; set; }
         
    }

    public class AppFilter : Entity, IAppFilter
    {
        public AppFilter()
        {
            _GuidID  = Guid.NewGuid().ToString();
        }
        private int _ID;
        public int ID
        {
            get { return _ID; }
            set { SetProperty(ref _ID, value); }
        }
        private string _GuidID;
        public string GuidID
        {
            get { return _GuidID; }
            set { SetProperty(ref _GuidID, value); }
        }
        private string _FieldName;
        public string FieldName
        {
            get { return _FieldName; }
            set { SetProperty(ref _FieldName, value); }
        }
        private string _Operator;
        public string Operator
        {
            get { return _Operator; }
            set { SetProperty(ref _Operator, value); }
        }

        private object _FilterValue;
        public object FilterValue
        {
            get { return _FilterValue; }
            set { SetProperty(ref _FilterValue, value); }
        }
        private string _valueType;
        public string valueType
        {
            get { return _valueType; }
            set { SetProperty(ref _valueType, value); }
        }
        private string _FilterValue1;
        public string FilterValue1
        {
            get { return _FilterValue1; }
            set { SetProperty(ref _FilterValue1, value); }
        }
      
        private Type _FieldType;
        public Type FieldType
        {
            get { return _FieldType; }
            set { SetProperty(ref _FieldType, value); }
        }


    }
    public class QueryBuild : Entity
    {
        public QueryBuild()
        {
            _GuidID = Guid.NewGuid().ToString();
            Entities = new List<string>();
            Fields = new List<string>();
        }
        private int _ID;
        public int ID
        {
            get { return _ID; }
            set { SetProperty(ref _ID, value); }
        }
        private string _GuidID;
        public string GuidID
        {
            get { return _GuidID; }
            set { SetProperty(ref _GuidID, value); }
        }
        private List<string> _Fields;
        public List<string> Fields
        {
            get { return _Fields; }
            set { SetProperty(ref _Fields, value); }
        }
        private List<string> _Entities;
        public List<string> Entities
        {
            get { return _Entities; }
            set { SetProperty(ref _Entities, value); }
        }
     

        private string _FieldsString;
        public string FieldsString
        {
            get { return _FieldsString; }
            set { SetProperty(ref _FieldsString, value); }
        }
       
        private string _EntitiesString;
        public string EntitiesString
        {
            get { return _EntitiesString; }
            set { SetProperty(ref _EntitiesString, value); }
        }
       
        private string _WhereCondition;
        public string WhereCondition
        {
            get { return _WhereCondition; }
            set { SetProperty(ref _WhereCondition, value); }
        }
      
        private string _OrderbyCondition;
        public string OrderbyCondition
        {
            get { return _OrderbyCondition; }
            set { SetProperty(ref _OrderbyCondition, value); }
        }

      
        private string _HavingCondition;
        public string HavingCondition
        {
            get { return _HavingCondition; }
            set { SetProperty(ref _HavingCondition, value); }
        }
      
        private string _GroupbyCondition;
        public string GroupbyCondition
        {
            get { return _GroupbyCondition; }
            set { SetProperty(ref _GroupbyCondition, value); }
        }
        
    }
    public class FilterType
    {
        public FilterType(string pfiltertype)
        {
            FilterDisplay = pfiltertype;
            FilterValue = pfiltertype;
        }
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string FilterDisplay { get; set; }
        public  string FilterValue { get; set; }
    }
}