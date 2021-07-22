using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.DataBase
{
    public interface IQueryFieldsandValues
    {

         IEntityField CompareField1 { get; set; }
         string Fieldvalue { get; set; }
         string Comparison { get; set; }
    }
}
