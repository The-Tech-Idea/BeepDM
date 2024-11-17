
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheTechIdea.Beep.DataBase;
using System.Threading.Tasks;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.WebAPI
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
