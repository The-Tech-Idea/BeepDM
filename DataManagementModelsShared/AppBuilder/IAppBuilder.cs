using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.AppBuilder
{
    public interface IAppBuilder
    {
        IApp App { get; set; }
        IDMEEditor DMEEditor { get; set; }
        bool Winform { get; set; }
        bool WPF { get; set; }

        bool Web { get; set; }
        bool Andriod { get; set; }
        bool IOS { get; set; }
        bool BuildApp(IDMEEditor dMEEditor,PassedArgs passedArgs);
    }

}
