using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.DataManagment_Engine.Report;

namespace TheTechIdea.DataManagment_Engine.AppBuilder
{
    public interface IAppScreen
    {
        string screenname { get; set; }
        string datasourcename { get; set; }
        string description { get; set; }
        List<IAppField> fields { get; set; }
        string imageLogoName { get; set; }
        List<IReportDefinition> Reports { get; set; }
        string subTitle { get; set; }
        string title { get; set; }
        PassedArgs args { get; set; }
        string parentscreen { get; set; }
        IAppComponent appComponentType { get; set; }
    }

}
