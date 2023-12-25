using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.Addin
{
   /// <summary>Represents an extension for a function.</summary>
 public  interface IFunctionExtension
    {
        IDMEEditor DMEEditor { get; set; }
        IPassedArgs Passedargs { get; set; }
    }
}
