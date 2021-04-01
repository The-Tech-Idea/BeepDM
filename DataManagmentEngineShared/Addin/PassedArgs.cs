

using System;
using System.Collections.Generic;
using TheTechIdea.DataManagment_Engine.DataBase;

namespace TheTechIdea
{
    public class PassedArgs

    {
        public IDataSource RDBSource { get; set; }
        public List<ObjectItem> Objects { get; set; } = new List<ObjectItem>();
        public IDM_Addin Addin { get; set; }
        public IDMDataView DMView {get;set;}
        public string CurrentTable { get; set; }
        public string ObjectType { get; set; }
        public string AddinName { get; set; }
        public string ObjectName { get; set; }
        public string AddinType { get; set; }
        public string EventType { get; set; }
        public string DatasourceName { get; set; }
        public int    Id { get; set; }
        public PassedArgs()
        {

        }
    }
    public class ObjectItem
    {
        public Object obj { get; set; }
        public string Name { get; set; }
        public ObjectItem()
        {

        }
    }
}
