using System;
using System.Collections.Generic;

using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;


namespace TheTechIdea.Beep.Workflow.Mapping
{
    public class EntityDataMap : Entity
    {
        public EntityDataMap()
        {
            GuidID = Guid.NewGuid().ToString();
            EntityFields = new List<EntityField>();
            MappedEntities = new List<EntityDataMap_DTL>();
        }


        private string _guidid;
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        }

        private int _id;
        public int id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _mappingname;
        public string MappingName
        {
            get { return _mappingname; }
            set { SetProperty(ref _mappingname, value); }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set { SetProperty(ref _description, value); }
        }

        private string _entityname;
        public string EntityName
        {
            get { return _entityname; }
            set { SetProperty(ref _entityname, value); }
        }

        private string _entitydatasource;
        public string EntityDataSource
        {
            get { return _entitydatasource; }
            set { SetProperty(ref _entitydatasource, value); }
        }
        private List<EntityField> _entityFields;
        public List<EntityField> EntityFields
        {
            get { return _entityFields; }
            set { SetProperty(ref _entityFields, value); }
        }
        private List<EntityDataMap_DTL> _mappedEntities;
        public List<EntityDataMap_DTL> MappedEntities
        {
            get { return _mappedEntities; }
            set { SetProperty(ref _mappedEntities, value); }
        }


    }

    public class EntityDataMap_DTL : Entity
    {
        public EntityDataMap_DTL()
        {
            Filter = new List<AppFilter>();
            EntityFields = new List<EntityField>();
            SelectedDestFields = new List<EntityField>();
            FieldMapping = new List<Mapping_rep_fields>();
            GuidID = Guid.NewGuid().ToString();

        }

        private string _guidid;
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        }

        private string _entitydatasource;
        public string EntityDataSource
        {
            get { return _entitydatasource; }
            set { SetProperty(ref _entitydatasource, value); }
        }
        public List<AppFilter> Filter { get; set; }

        private string _entityname;
        public string EntityName
        {
            get { return _entityname; }
            set { SetProperty(ref _entityname, value); }
        }

        private List<EntityField> _entityFields;
        public List<EntityField> EntityFields
        {
            get { return _entityFields; }
            set { SetProperty(ref _entityFields, value); }
        }

        private List<EntityField> _selectedDestFields;
        public List<EntityField> SelectedDestFields
        {
            get { return _selectedDestFields; }
            set { SetProperty(ref _selectedDestFields, value); }
        }

        private List<Mapping_rep_fields> _fieldMapping;
        public List<Mapping_rep_fields> FieldMapping
        {
            get { return _fieldMapping; }
            set { SetProperty(ref _fieldMapping, value); }
        }
    }

}
