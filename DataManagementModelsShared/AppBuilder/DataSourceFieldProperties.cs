using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.AppBuilder
{
   public class DataSourceFieldProperties
    {
        public DataSourceFieldProperties()
        {

          
        }
        public string DatasourceName { get; set; }
        public List<DataSourceEntityProperties> enitities { get; set; } = new List<DataSourceEntityProperties>();

    }
    public class DataSourceEntityProperties
    {
        public DataSourceEntityProperties()
        {

        }
        public string entity { get; set; }
        public List<AppField> properties { get; set; } = new List<AppField>();
    }
}
