using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.DataManagment_Engine.Vis
{
    public interface IAddinVisSchema :IOrder,INode, IBranchID
    {
        string RootNodeName { get; set; }
        string CatgoryName { get; set; }
     

    }
}
