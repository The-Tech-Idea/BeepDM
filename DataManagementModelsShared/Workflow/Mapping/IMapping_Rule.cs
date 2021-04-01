using System.Collections.Generic;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Workflow
{
    public interface IMapping_Rule
    {
        string RuleName { get; set; }
        string Rule { get; set; }
        PassedArgs ExecuteRule(PassedArgs args);
    }
}