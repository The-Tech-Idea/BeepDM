using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Logger;
using System;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Editor;

using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Addin
{
    /// <summary>
    /// Represents an add-in in the Beep system.
    /// </summary>
  public interface IDM_Addin
    {
        string ParentName { get; set; }
        string ObjectName { get; set; } 
        string ObjectType { get; set; }
        string AddinName { get; set; }
        string Description { get; set; }
        Boolean DefaultCreate { get; set; }
        string DllPath { get; set; }
        string DllName { get; set; }
        string NameSpace { get; set; }
      
        IErrorsInfo ErrorObject { get; set; }
        IDMLogger Logger { get; set; }
        IDMEEditor DMEEditor { get; set; }
        EntityStructure EntityStructure { get; set; }
        string EntityName { get; set; }
        IPassedArgs Passedarg { get; set; }
       
        void Run(IPassedArgs pPassedarg);
        void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil,string[] args, IPassedArgs e, IErrorsInfo per );
        
    }
}
