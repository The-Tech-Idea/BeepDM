using System.Collections.Generic;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Workflow
{
    public interface IMapping_Rule
    {
        string RuleName { get; set; }
        string Rule { get; set; }
        PassedArgs ExecuteRule(PassedArgs args);
    }
}