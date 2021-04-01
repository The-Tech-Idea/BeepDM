using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace TheTechIdea.DataManagment_Engine.DataBase
{
    public class RDBSourceADP
    {
        public RDBSourceADP()
        {

        }
        public IDataSource dataSource { get; set; }
        public IDataAdapter dataAdapter { get; set; }
        public DataSet dataSet { get; set; }
    }
}
