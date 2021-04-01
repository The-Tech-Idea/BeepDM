
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheTechIdea.DataManagment_Engine.DataBase;
using System.Threading.Tasks;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.WebAPI
{
    public interface IWebAPIDataSource : IDataSource
    {
       
        List<EntityField> Fields { get; set; }
        
        string          ApiKey { get; set; }
        string          Resource { get; set; }
        Dictionary<string,string> Parameters { get; set; }
        Task<List<object>> ReadData(bool HeaderExist, int fromline = 0, int toline = 100);
    

    }
}
