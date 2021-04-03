using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace TheTechIdea.DataManagment_Engine.Vis
{
    public class DisplayPanel:IDisplayPanel
    {
        public DisplayPanel()
        {

        }

        public object DisplayBack { get ; set ; }

        public void AddControl(object control)
        {
            Control b = (Control)DisplayBack;
            b.Controls.Add((Control)control);
        }

        public void RmoveControl(object control)
        {
            Control b = (Control)DisplayBack;
            b.Controls.Remove((Control)control);
        }
    }

}
