using System;
using System.Collections.Generic;
using System.Diagnostics;
using TheTechIdea.Beep.Rules.BuiltinParsers;

namespace TheTechIdea.Beep.Rules
{
    public partial class RuleEngine
    {
        private static RuleExecutionPolicy CaptureNfelPolicy(RuleExecutionPolicy policy)
        {
            policy ??= RuleExecutionPolicy.Default;
            if (policy.MaxDepth < 0 || policy.MaxExecutionMs < 0 || !Enum.IsDefined(policy.MinimumLifecycleState))
                throw new RuleEvaluationException(DiagnosticCode.PolicyViolation, "NFEL policy profile is invalid.");
            var sourceTypes = policy.AllowedTokenTypes;
            HashSet<TokenType> allowed = null;
            if (sourceTypes != null)
            {
                if (sourceTypes.Count > Enum.GetValues<TokenType>().Length)
                    throw new RuleEvaluationException(DiagnosticCode.PolicyViolation, "NFEL token policy is invalid.");
                try { allowed = new HashSet<TokenType>(sourceTypes); }
                catch (Exception) { throw new RuleEvaluationException(DiagnosticCode.PolicyViolation, "NFEL token policy capture failed."); }
                if (allowed.Count != sourceTypes.Count)
                    throw new RuleEvaluationException(DiagnosticCode.PolicyViolation, "NFEL token policy changed during capture.");
                foreach (var type in allowed)
                    if (!Enum.IsDefined(type))
                        throw new RuleEvaluationException(DiagnosticCode.PolicyViolation, "NFEL token policy is invalid.");
            }
            return new RuleExecutionPolicy
            {
                MaxDepth = policy.MaxDepth, MaxExecutionMs = policy.MaxExecutionMs,
                MinimumLifecycleState = policy.MinimumLifecycleState,
                AllowDeprecatedExecution = policy.AllowDeprecatedExecution,
                AllowedTokenTypes = allowed
            };
        }

        private object EvaluateNfel(IList<Token> tokens, Dictionary<string, object> parameters, RuleExecutionPolicy policy)
        {
            var clock = Stopwatch.StartNew();
            policy = CaptureNfelPolicy(policy);
            try { return NfelExpression.FromTokens(tokens).Evaluate(parameters, policy, clock); }
            catch (NfelSyntaxException error) { throw NfelParseFailure(error); }
        }

        private (Dictionary<string, object> outputs, object result) SolveNfel(
            string key, IRule rule, Dictionary<string, object> parameters, RuleExecutionPolicy policy)
        {
            var clock = Stopwatch.StartNew();
            try
            {
                string currentText, suppliedText, suppliedVersion;
                IRuleStructure supplied;
                List<Token> suppliedTokens;
                RuleLifecycleState state;
                try
                {
                    currentText = rule.RuleText;
                    supplied = rule.Structure;
                    suppliedText = supplied?.Expression;
                    suppliedVersion = supplied?.SchemaVersion;
                    suppliedTokens = supplied?.Tokens;
                    state = supplied?.LifecycleState ?? RuleLifecycleState.Draft;
                }
                catch (Exception) { throw new RuleEvaluationException(DiagnosticCode.PolicyViolation, "NFEL rule capture failed."); }
                if (!Enum.IsDefined(state) || state < policy.MinimumLifecycleState ||
                    (state == RuleLifecycleState.Deprecated && !policy.AllowDeprecatedExecution))
                    throw new RuleEvaluationException(DiagnosticCode.LifecycleStateViolation, "NFEL rule lifecycle denied execution.");
                if (!string.Equals(key, currentText, StringComparison.Ordinal))
                    throw NfelExpression.Syntax(DiagnosticCode.UnexpectedToken, 0, 0);
                var expression = NfelExpression.Parse(key);
                if (supplied != null)
                {
                    if (!string.Equals(suppliedVersion, RuleStructure.CurrentSchemaVersion, StringComparison.Ordinal) ||
                        (!string.IsNullOrEmpty(suppliedText) && !string.Equals(suppliedText, key, StringComparison.Ordinal)) ||
                        (suppliedTokens?.Count > 0 && !expression.Matches(suppliedTokens)))
                        throw NfelExpression.Syntax(DiagnosticCode.UnexpectedToken, 0, 0);
                }
                // Execution uses the private tree; publishing tokens cannot mutate the admitted run.
                if (supplied == null)
                {
                    try { rule.Structure = new RuleStructure { Expression = key, Tokens = expression.CopyTokens(), Rulename = "Nfel", RuleType = "NfelParser" }; }
                    catch (Exception) { throw new RuleEvaluationException(DiagnosticCode.PolicyViolation, "NFEL rule publication failed."); }
                }
                return (parameters ?? new Dictionary<string, object>(), expression.Evaluate(parameters, policy, clock));
            }
            catch (NfelSyntaxException error) { throw NfelParseFailure(error); }
            catch (RuleException) { throw; }
            catch (Exception) { throw new RuleEvaluationException(DiagnosticCode.PolicyViolation, "NFEL rule capture failed."); }
        }

        private static RuleParseException NfelParseFailure(NfelSyntaxException error) =>
            new(new[] { error.Diagnostic }, "NFEL expression admission failed.");
    }
}
