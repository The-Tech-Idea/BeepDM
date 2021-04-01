using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.DataManagment_Engine.AppBuilder;
using TheTechIdea.DataManagment_Engine.DataBase;

namespace TheTechIdea.DataManagment_Engine.ConfigUtil
{
   public interface IDatasourceEntities
    {
        string datasourcename { get; set; }
        List<EntityStructure> Entities { get; set; }
       
    }
    public class DatasourceEntities: IDatasourceEntities
    {
        public DatasourceEntities()
        {
           
            Entities = new List<EntityStructure>();
        }

        public string datasourcename { get ; set ; }
        public List<EntityStructure> Entities { get ; set ; }
     

        
    }
}
