using System;
using System.Collections.Generic;

using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Rules
{
    public interface IRule
    {

        /// <summary>
        /// The textual expression that defines the rule's logic.
        /// For example: ":Entity1.LastName == 'Smith'" or other valid expressions.
        /// </summary>
        string RuleText { get; set; }
        RuleStructure Structure { get; set; } 
        /// <summary>
        /// Solves the specified rule using the provided parameters and returns the outputs and the overall result.
        /// </summary>
        /// <param name="ruleName">The name of the rule to evaluate.</param>
        /// <param name="parameters">Parameters required for rule evaluation.</param>
        /// <returns>
        /// A tuple where:
        /// - <c>outputs</c> is a dictionary containing key/value pairs representing the rule outputs.
        /// - <c>result</c> is an object representing the overall result of the rule evaluation.
        /// </returns>
        (Dictionary<string, object> outputs, object result) SolveRule( Dictionary<string, object> parameters=null);
    }

}
