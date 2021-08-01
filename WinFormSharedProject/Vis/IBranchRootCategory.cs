using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Vis
{
    public interface IBranchRootCategory
    {
        IErrorsInfo CreateCategoryNode(CategoryFolder p);
        
    }
}
