using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.AppBuilder
{
   public interface IAppComponent
    {
        string ComponentName { get; set; }
         AppComponentType ComponentType { get; set; }
        IDMEEditor DMEEditor { get; set; }
        int DisplayOrder { get; set; }
     
    }
}
