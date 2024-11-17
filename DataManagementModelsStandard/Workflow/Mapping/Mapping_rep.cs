using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Workflow
{
    public class Map_Schema : Entity, IMap_Schema
    {
        public Map_Schema()
        {

            GuidID = Guid.NewGuid().ToString();
            Maps = new List<EntityDataMap>();

        }

        private string _guidid;
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        }

        private int _id;
        public int Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _schemaname;
        public string SchemaName
        {
            get { return _schemaname; }
            set { SetProperty(ref _schemaname, value); }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set { SetProperty(ref _description, value); }
        }
        private List<EntityDataMap> _maps;
        public List<EntityDataMap> Maps 
         {
           get { return _maps; }
           set { SetProperty(ref _maps, value);}
        }
    }


}
