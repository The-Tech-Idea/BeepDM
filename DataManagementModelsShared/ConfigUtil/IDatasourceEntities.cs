using System;
using System.Collections.Generic;
using System.Text;

using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.ConfigUtil
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
