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

        // Operators
        Plus,            // +
        Minus,           // -
        Multiply,        // *
        Divide,          // /
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
        Unknown
    }

}
