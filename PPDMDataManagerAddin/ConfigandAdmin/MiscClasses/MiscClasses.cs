using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleODM.systemconfigandutil.MiscClasses
{
    public class Obs_no
        {
        public Obs_no()
        {

        }
        public  string EntityName { get; set; }
    public  string EntityKey { get; set; }
public  int Maxvalue { get; set; }
        }


    public class ChangedObject
    {
        public string ContextName { get; set; } = "";
        public string EntityName { get; set; } = "";
        public string ChangeType { get; set; } = "";
        public ObjectStateEntry EntityObj;

        public ChangedObject()
        {
        }
        public ChangedObject(ObjectStateEntry pEntityObj, string pChangeType, string pContextName)
        {
            EntityName = pEntityObj.EntitySet.Name.ToString();
            ChangeType = pChangeType;
            ContextName = pContextName;
            EntityObj = pEntityObj;
        }
    }

    public class ListofObjectsStateEntryPerConext
    {
        public string ContextName { get; set; } = "";
        public List<ChangedObject> ListofObjects { get; set; } = new List<ChangedObject>();
        public ListofObjectsStateEntryPerConext()
        {
        }
        public ListofObjectsStateEntryPerConext(string pEntityContextname)
        {
            ContextName = pEntityContextname;
        }
    }

    public class RPPDM_ROW_QUALITY_CLS
    {
        public string ROW_QUALITY_ID { get; set; }
        public string SHORT_NAME { get; set; }
    }

    public class RSOURCE_CLS
    {
        public string SOURCE { get; set; }
        public string SHORT_NAME { get; set; }
    }

    public class RWELL_LEVEL_TYPE_CLS
    {
        public string WELL_LEVEL_TYPE { get; set; }
        public string SHORT_NAME { get; set; }
    }

    public class RWELL_XREF_TYPE_CLS
    {
        public string XREF_TYPE { get; set; }
        public string SHORT_NAME { get; set; }
    }

    public class cls_MemoryInfo
    {
        public string TotalPhysicalMemory { get; set; }
        public string AvailablePhysicalMemory { get; set; }
        public string TotalVirtualMemory { get; set; }
        public string AvailableVirtualMemory { get; set; }
        public cls_MemoryInfo()
        {
            GetMemInfo();
        }
        public void GetMemInfo()
        {
            //TotalPhysicalMemory = System.Math.Round(Environment.Computer.Info.TotalPhysicalMemory / (double)(1024 * 1024));
            //AvailablePhysicalMemory = System.Math.Round(My.Computer.Info.AvailablePhysicalMemory / (double)(1024 * 1024));
            //TotalVirtualMemory = System.Math.Round(My.Computer.Info.TotalVirtualMemory / (double)(1024 * 1024));
            //AvailableVirtualMemory = System.Math.Round(My.Computer.Info.AvailableVirtualMemory / (double)(1024 * 1024));
        }
    }

}
