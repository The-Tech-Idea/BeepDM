using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinParsers
{
    /// <summary>
    /// Bounded NFEL arithmetic, comparison, Boolean and lazy ternary parser.
    /// Dotted identifiers are parameter keys. See Rules/NFEL.md for the profile.
    /// </summary>
    [RuleParser(parserKey: "NfelParser")]
    public sealed class NfelParser : IRuleParser
    {
        public const string LanguageProfile = "NFEL-1";
        public const int MaximumSourceLength = NfelExpression.MaxSourceLength;
        public const int MaximumTokenCount = NfelExpression.MaxTokens;
        public const int MaximumNodeCount = NfelExpression.MaxNodes;
        public const int MaximumDepth = NfelExpression.MaxDepth;
        public const int MaximumStringLength = NfelExpression.MaxStringLength;
        public const int MaximumRetainedStructures = 128;
        public const int MaximumRetainedCharacters = 1048576;
        public const int MaximumRetainedTokens = 32768;

        private readonly object _historyLock = new();
        private readonly Queue<RuleStructure> _history = new();
        private int _characters, _tokenCount;

        List<IRuleStructure> IRuleParser.RuleStructures
        {
            get
            {
                lock (_historyLock)
                {
                    var result = new List<IRuleStructure>(_history.Count);
                    foreach (var item in _history) result.Add(Copy(item));
                    return result;
                }
            }
        }

        public ParseResult ParseRule(string expression)
        {
            try
            {
                var admitted = NfelExpression.Parse(expression);
                var structure = new RuleStructure
                {
                    Expression = expression, Tokens = admitted.CopyTokens(),
                    Rulename = "Nfel", RuleType = "NfelParser"
                };
                Retain(structure);
                return new ParseResult { Success = true, Structure = structure };
            }
            catch (NfelSyntaxException error)
            {
                return new ParseResult { Success = false, Diagnostics = new List<ParseDiagnostic> { error.Diagnostic } };
            }
        }

        public ParseResult ParseRule(IRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            return ParseRule(rule.RuleText);
        }

        public void Clear()
        {
            lock (_historyLock) { _history.Clear(); _characters = _tokenCount = 0; }
        }

        private void Retain(RuleStructure structure)
        {
            var owned = Copy(structure);
            lock (_historyLock)
            {
                while (_history.Count >= MaximumRetainedStructures ||
                       _characters > MaximumRetainedCharacters - owned.Expression.Length ||
                       _tokenCount > MaximumRetainedTokens - owned.Tokens.Count)
                {
                    var removed = _history.Dequeue();
                    _characters -= removed.Expression.Length;
                    _tokenCount -= removed.Tokens.Count;
                }
                _history.Enqueue(owned);
                _characters += owned.Expression.Length;
                _tokenCount += owned.Tokens.Count;
            }
        }

        private static RuleStructure Copy(RuleStructure source) => new()
        {
            GuidID = source.GuidID, Rulename = source.Rulename, RuleType = source.RuleType,
            Expression = source.Expression, SchemaVersion = source.SchemaVersion,
            CreatedUtc = source.CreatedUtc, UpdatedUtc = source.UpdatedUtc,
            LifecycleState = source.LifecycleState, Author = source.Author,
            Tags = source.Tags, Module = source.Module,
            Tokens = source.Tokens.ConvertAll(token => new Token(token.Type, token.Value, token.Start, token.Length) { SchemaVersion = token.SchemaVersion })
        };
    }
}
