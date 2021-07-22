using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Editor
{
    public interface IDataSourceDataType
    {
        int Size1 { get; set; }
        int Size2 { get; set; }
        List<string> DataTypes { get; set; }
    }
}
