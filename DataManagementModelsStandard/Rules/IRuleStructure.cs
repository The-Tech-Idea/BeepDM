using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.Rules
{
    public interface IRuleStructure
    {
        string GuidID { get; set; }
        string Rulename { get; set; }
        string Expression { get; set; }
        string RuleType { get; set; }
        List<Token> Tokens { get; set; }
        string SchemaVersion { get; set; }
        DateTime CreatedUtc { get; set; }
        DateTime UpdatedUtc { get; set; }

        /// <summary>Comma-separated tags for catalog discovery queries.</summary>
        string Tags { get; set; }

        /// <summary>The module or subsystem this rule belongs to.</summary>
        string Module { get; set; }

        string Author { get; set; }

        /// <summary>Governance lifecycle state of this rule.</summary>
        RuleLifecycleState LifecycleState { get; set; }

        /// <summary>
        /// Returns true when this structure's <see cref="SchemaVersion"/> is compatible
        /// with the current engine version.
        /// </summary>
        bool IsCompatibleVersion(string engineVersion);
    }
}
