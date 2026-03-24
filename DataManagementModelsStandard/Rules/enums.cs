using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Rules
{
    public enum TokenType
    {
        // Special prefixed tokens
        EntityField,     // e.g., :Entity1.LastName
        RuleReference,   // e.g., @AnotherRule

        // Literals
        StringLiteral,   // "some text" or 'some text'
        NumericLiteral,  // e.g., 123 or 45.67
        BooleanLiteral,  // true or false
        NullLiteral,     // null

        // Operators
        Plus,            // +
        Minus,           // -
        Multiply,        // *
        Divide,          // /
        Modulo,          // %
        Equal,           // ==
        NotEqual,        // != or <>
        GreaterThan,     // >
        LessThan,        // <
        GreaterEqual,    // >=
        LessEqual,       // <=
        And,             // && or AND
        Or,              // || or OR
        Not,             // ! or NOT

        // Grouping/Separators
        LeftParenthesis, // (
        RightParenthesis,// )
        Comma,           // ,

        // Identifiers for functions or variables (if needed)
        Identifier,

        // Unknown token type
        Unknown,
        OpenParen,
        CloseParen
    }

    /// <summary>Severity level for parse and evaluation diagnostics.</summary>
    public enum DiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>Structured diagnostic codes for tokenizer/parser/runtime failures.</summary>
    public enum DiagnosticCode
    {
        None = 0,

        // Tokenizer errors (1xx)
        UnknownToken = 100,
        UnterminatedString = 101,
        InvalidNumeric = 102,
        InvalidOperator = 103,

        // Parser errors (2xx)
        MismatchedParenthesis = 200,
        UnexpectedToken = 201,
        EmptyExpression = 202,
        InvalidPrecedence = 203,

        // Runtime errors (3xx)
        RuleNotFound = 300,
        CircularRuleReference = 301,
        RecursionDepthExceeded = 302,
        NullDereference = 303,
        DivisionByZero = 304,
        TypeCoercionFailed = 305,
        IdentifierNotFound = 306,
        EntityFieldNotFound = 307,
        UnsupportedOperator = 308,

        // Policy / governance (4xx)
        PolicyViolation = 400,
        LifecycleStateViolation = 401,
        MaxDepthExceeded = 402,
        TimeoutExceeded = 403,
        OperatorNotAllowed = 404,

        // Catalog errors (5xx)
        DuplicateRuleRegistration = 500,
        DuplicateParserRegistration = 501,
        CatalogKeyInvalid = 502
    }

    /// <summary>Lifecycle governance states for rules.</summary>
    public enum RuleLifecycleState
    {
        Draft,
        Review,
        Approved,
        Deprecated
    }
}
