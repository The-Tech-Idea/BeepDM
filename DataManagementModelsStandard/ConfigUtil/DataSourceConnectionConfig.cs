
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Utilities;


namespace TheTechIdea.Beep.ConfigUtil
{
    public class DataSourceConnectionConfig : Entity
    {
        public DataSourceConnectionConfig()
        {
            GuidID = Guid.NewGuid().ToString();
        }

        private int _id;
        public int ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _guidid;
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        }

        private string _datasourcename;
        public string DataSourceName
        {
            get { return _datasourcename; }
            set { SetProperty(ref _datasourcename, value); }
        }
        public DatasourceCategory datasourceCategory { get; set; }

        private List<string> _connectiondrivers = new List<string>();
        public List<string> ConnectionDrivers
        {
            get { return _connectiondrivers; }
            set { SetProperty(ref _connectiondrivers, value); }
        }

    }
}
