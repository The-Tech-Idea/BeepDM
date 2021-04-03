using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.DataManagment_Engine.AI
{
    public class AccordML : IAAPP
    {
        public AccordML()
        {
            AppID = Guid.NewGuid().ToString();
        }
        public string AppName { get; set; }
        public string AppID { get ; }
        public string DataSourceName { get ; set ; }
        public List<string> Tables { get ; set ; }
        public List<DataTable> TestData { get; set; }
        public IDMEEditor DMEEditor { get ; set ; }

        public bool Evaluate()
        {
            throw new NotImplementedException();
        }

        public bool Predict(object PredictionData)
        {
            throw new NotImplementedException();
        }

        public bool Train()
        {
            throw new NotImplementedException();
        }

    }
}
