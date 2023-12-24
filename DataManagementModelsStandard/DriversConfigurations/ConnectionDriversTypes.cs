using System;


namespace DataManagementModels.DriversConfigurations
{
    public class ConnectionDriversTypes
    {
        public ConnectionDriversTypes()
        {
            GuidID = Guid.NewGuid().ToString();
        }
        public int ID { get; set; }
        public string GuidID { get; set; }
        public string PackageName { get; set; }
        public string DriverClass { get; set; }
        public string version { get; set; }
        public string dllname { get; set; }
        public Type AdapterType { get; set; }
        public Type CommandBuilderType { get; set; }
        public Type DbConnectionType { get; set; }

    }
}
