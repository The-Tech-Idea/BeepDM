using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.Rules
{
    public interface IRuleStructure
    {
         string Rulename { get; set; }
      
         string Expression { get; set; }
        string RuleType { get; set; }
        // New property to hold the parsed tokens or AST
        List<Token> Tokens { get; set; }
    }
}
