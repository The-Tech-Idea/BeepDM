using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.DataBase
{
    public class DatatypeMapping : IDatatypeMapping
    {
        public DatatypeMapping()
        {

        }
       
        public string DataType { get; set; } 
        public string DataSourceName { get ; set ; }
        public string NetDataType { get ; set ; }
        public bool Fav { get; set ; }   

     
    }
}
