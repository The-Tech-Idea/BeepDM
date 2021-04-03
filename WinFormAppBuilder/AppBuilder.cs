using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.AppBuilder
{
   
    public class AppBuilder : IAppBuilder
    {
        public AppBuilder()
        {

        }
        public AppBuilder(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor;
        }
        public IDMEEditor DMEEditor { get; set; }
        public IApp App { get; set; }
        public bool Winform { get; set; } = true;
        public bool Web { get; set; } = false;
        public bool Andriod { get; set; } = false;
        public bool IOS { get; set; } = false;
        public bool WPF { get; set; } = false;
        public bool BuildApp()
        {
            try

            {



                DMEEditor.AddLogMessage("Success", $"Generating App {App.AppName}", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                string errmsg = "Error Generating App";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }

        }

    }
}
