using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Rules
{
    public interface IRuleParserFactory
    {
        IRuleParser GetParser(IRuleStructure ruleStructure);
    }

}
