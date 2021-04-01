using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.AppBuilder
{
    public class App : IApp
    {
       
        public App()
        {
            ID = Guid.NewGuid().ToString();
            CreateDate = DateTime.Now;
            Ver = 1;
            initBreadCrumb();
        }
        public App(string pAppName, string pDataViewDataSourceName, AppType pApptype)
        {
            ID = Guid.NewGuid().ToString();
            AppName = pAppName;
            DataViewDataSourceName = pDataViewDataSourceName;
            Apptype = pApptype;
            CreateDate = DateTime.Now;
            Ver = 1;
            initBreadCrumb();
        }
        public App(string pAppName, string pDataViewDataSourceName, AppType pApptype, string pOuputFolder)
        {
            ID = Guid.NewGuid().ToString();
            AppName = pAppName;
            DataViewDataSourceName = pDataViewDataSourceName;
            Apptype = pApptype;
            CreateDate = DateTime.Now;
            OuputFolder = pOuputFolder;
            Ver = 1;
            initBreadCrumb();
        }
        private void initBreadCrumb()
        {
            BreadCrumb x = new BreadCrumb();
            x.screenname = "HOME";
            breadCrumb.AddFirst(x);
        }
        public string ID { get; set; }
        public string AppName { get ; set ; }
        public string DataViewDataSourceName { get ; set ; }
        public DateTime CreateDate { get ; set ; }
        public DateTime UpdateDate { get ; set ; }
        public int Ver { get ; set ; }
        public AppType Apptype { get ; set ; }
        public string OuputFolder { get; set; }
        public string startupscrreen { get; set; }
        public List<AppScreen> screens { get; set; } = new List<AppScreen>();
        public List<ReportTemplate> Reports { get; set; } = new List<ReportTemplate>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public string ImageLogoName { get; set; }
        public string AppTitle { get; set; }
        public string AppSubTitle { get; set; }
        public string AppDescription { get; set; }
        public LinkedList<BreadCrumb> breadCrumb { get; set; } = new LinkedList<BreadCrumb>();
        public List<AppVersion> AppVersions { get; set; } = new List<AppVersion>();
    }
}
