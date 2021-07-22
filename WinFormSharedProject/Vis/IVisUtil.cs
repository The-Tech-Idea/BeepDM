using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Tools;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.Beep.Vis
{
    public interface IVisUtil
    {
        PassedArgs Args { get; set; }
     

        IDMEEditor DMEEditor { get; set; }
        IErrorsInfo Erinfo { get; set; }
      
        IAssemblyHandler LLoader { get; set; }
        IDMLogger Logger { get; set; }
        ITree treeEditor { get; set; }
        IControlEditor controlEditor { get; set; }
       // event EventHandler<PassedArgs> OnObjectSelected;
      
        IErrorsInfo CheckSystemEntryDataisSet();
      

        Control DisplayPanel { get; set; }
        Form    ParentForm { get; set; }
        IDM_Addin ShowFormFromAddin( string formname, IDMEEditor pDMEEditor, string[] args, IPassedArgs e);
        IErrorsInfo ShowMainDisplayForm();
        IDM_Addin ShowUserControlPopUp(string usercontrolname, IDMEEditor pDMEEditor, string[] args, IPassedArgs e);
        IDM_Addin ShowUserControlInContainer(string usercontrolname,  Control Container, IDMEEditor pDMEEditor, string[] args, IPassedArgs e);
      
       




    }
}