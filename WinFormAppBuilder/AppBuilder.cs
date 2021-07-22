using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.Beep.AppBuilder
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
        IVisUtil Visutil { get; set; }
        public bool BuildApp(IDMEEditor dMEEditor, PassedArgs passedArgs)
        {
            try

            {
                DMEEditor = dMEEditor;

                Visutil =(IVisUtil) passedArgs.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
                if (Winform)
                {
                    Visutil.ShowUserControlPopUp("uc_WinformApp",  DMEEditor, new string[] { "" }, (PassedArgs)DMEEditor.Passedarguments);
                }


              //  DMEEditor.AddLogMessage("Success", $"Generating App {App.AppName}", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                string errmsg = "Error Generating App";
                MessageBox.Show("Fail", $"{errmsg}:{ex.Message}");
                return false;
            }

        }

    }
}
