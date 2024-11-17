using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Composite
{
    public class CompositeLayer : ILayer
    {
        public CompositeLayer()
        {
            Entities = new List<EntityStructure>();
            GuidID = Guid.NewGuid().ToString();
        }
        public CompositeLayer(string pLayerName, string pDataViewDataSourceName, string pLocalDBDriver,string  pLocalDBDriverVersion)
        {
            Entities = new List<EntityStructure>();
            GuidID = Guid.NewGuid().ToString();
        }
        public List<EntityStructure> Entities { get; set; }
        public IDataSource DataSource { get ; set ; }
        public string DataViewDataSourceName { get ; set ; }
        public string DataSourceName { get ; set ; }
        public string LocalDBDriver { get ; set ; }
        public string LocalDBDriverVersion { get ; set ; }
        public DateTime DateCreated { get ; set ; }
        public DateTime DateUpdated { get ; set ; }
        public string LayerName { get ; set ; }
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
    }
}
