﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Tools;
using TheTechIdea.Util;

namespace TheTechIdea.Winforms.VIS
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
        IErrorsInfo PreSetupAddins();
        IErrorsInfo CheckSystemEntryDataisSet();
        IErrorsInfo Run();

        Control DisplayPanel { get; set; }
        IDM_Addin ShowFormFromAddin( string formname, IDMEEditor pDMEEditor, string[] args, PassedArgs e);
        IErrorsInfo ShowMainDisplayForm();
        IDM_Addin ShowUserControlPopUp(string usercontrolname, IDMEEditor pDMEEditor, string[] args, PassedArgs e);
        IDM_Addin ShowUserControlInContainer(string usercontrolname,  Control Container, IDMEEditor pDMEEditor, string[] args, PassedArgs e);
      
       




    }
}