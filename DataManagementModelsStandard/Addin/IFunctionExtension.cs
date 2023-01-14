using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.Addin
{
   public  interface IFunctionExtension
    {
        IDMEEditor DMEEditor { get; set; }
        IPassedArgs Passedargs { get; set; }
    }
}
