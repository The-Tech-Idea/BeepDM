using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.Vis
{
    public interface IDisplayPanel
    {
        object DisplayBack { get; set; }
        void AddControl(object control);
        void RmoveControl(object control);

    }
}
