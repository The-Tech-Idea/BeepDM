using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.DataManagment_Engine.Editor
{
    public interface IDataSourceDataType
    {
        int Size1 { get; set; }
        int Size2 { get; set; }
        List<string> DataTypes { get; set; }
    }
}
