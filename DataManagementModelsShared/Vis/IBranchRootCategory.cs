using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Vis
{
    public interface IBranchRootCategory
    {
        IErrorsInfo CreateCategoryNode(CategoryFolder p);
        
    }
}
