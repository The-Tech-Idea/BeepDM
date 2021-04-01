using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.DataManagment_Engine.Report;

namespace TheTechIdea.DataManagment_Engine.AppBuilder
{
  
    public class AppScreen : IAppScreen
    {
        public AppScreen()
        {
            
        }
        public string screenname { get; set; }
        public string imageLogoName { get; set; }
        public string title { get; set; }
        public string subTitle { get; set; }
        public string description { get; set; }
        public string datasourcename { get; set; }
        public List<IAppField> fields { get; set; } = new List<IAppField>();
        public List<IReportDefinition> Reports { get; set; } = new List<IReportDefinition>();
        public PassedArgs args { get; set; } = new PassedArgs();
    }
}
