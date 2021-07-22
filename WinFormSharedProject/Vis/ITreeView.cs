using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Vis
{
    public interface ITreeView
    {
        object TreeStrucure { get; set; }
        IVisUtil Visutil { get; set; }
    }
}
