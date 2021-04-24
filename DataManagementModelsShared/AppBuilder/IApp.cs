using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.AppBuilder
{
    public interface IApp
    {
        string ID { get; set; }
        string AppName { get; set; }
        string DataViewDataSourceName { get; set; }
        DateTime CreateDate { get; set; }
        DateTime UpdateDate { get; set; }
        int Ver { get; set; }
        AppType Apptype { get; set; }
        string OuputFolder { get; set; }
        List<ReportTemplate> Reports { get; set; }
        List<EntityStructure> Entities { get; set; }
        string ImageLogoName { get; set; }
        string AppTitle { get; set; }
        string AppSubTitle { get; set; }
        string AppDescription { get; set; }
        List<AppVersion> AppVersions { get; set; }

        LinkedList<BreadCrumb> breadCrumb { get; set; }

        List<ConnectionProperties> dataConnections { get; set; }


    }
}
