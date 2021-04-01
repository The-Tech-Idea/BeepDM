using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.DataManagment_Engine.AppBuilder
{
    public interface IAppDesigner
    {
        IDMEEditor DMEEditor { get; set; }
        bool Winform { get; set; }
        bool WPF { get; set; }
        bool Web { get; set; }
        bool Andriod { get; set; }
        bool IOS { get; set; }
        List<IApp> Apps { get; set; }
        void StartDesign();
        void LoadDesign();
        void SaveDesign();

    }
}
