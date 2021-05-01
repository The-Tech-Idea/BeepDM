using DevExpress.Data.XtraReports.DataProviders;
using DevExpress.Snap.Core.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine;

namespace DXReportBuilder
{
    public  class DxDataSourceInfo
    {
        public IDMEEditor DMEEditor { get; set; }
        public List<IDataSource> dataSources { get; set; } = new List<IDataSource>();
        public DataSourceInfoCollection dxdatasources { get; set; } = new DataSourceInfoCollection();
        public DxDataSourceInfo()
        {

        }
        public DxDataSourceInfo(List<IDataSource> pdataSources)
        {
            dataSources = pdataSources;
        }
        public void ConvertBeepDataSource(List<IDataSource> pdataSources)
        {
            dataSources = pdataSources;
            ConvertBeepDataSource();
        }
        public void ConvertBeepDataSource()
        {
            foreach (IDataSource ds in dataSources)
            {
                foreach (string ent in ds.GetEntitesList())
                {
                  
                    dxdatasources.Add(ent,ds.GetEntity(ent,null));
                } 

            }
        }

    }
}
