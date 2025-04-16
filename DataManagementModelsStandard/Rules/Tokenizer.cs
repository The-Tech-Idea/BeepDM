using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Rules
{
    public class Tokenizer
    {
        private readonly string _input;
        private int _position;

        public Tokenizer(string input)
        {
            _input = input;
            _position = 0;
        }


        public List<Token> Tokenize()
        {
            List<Token> tokens = new List<Token>();

            while (!IsAtEnd())
            {
                SkipWhitespace();
                if (IsAtEnd())
                    break;

                char current = Peek();

                // Special prefixes:
                if (current == ':')
                    tokens.Add(ParseEntityFieldToken());
                else if (current == '@')
                    tokens.Add(ParseRuleReferenceToken());
                // Literal cases: string literal if it starts with a quote, numeric literal if digit/dot.
                else if (current == '"' || current == '\'')
                    tokens.Add(ParseStringLiteralToken());
                else if (Char.IsDigit(current) || (current == '.' && LookaheadIsDigit()))
                    tokens.Add(ParseNumericLiteralToken());
                else if (current == '#')
                    // We can treat # literals as string literals (or use a dedicated branch)
                    tokens.Add(ParseHashLiteralToken());
                else if (IsOperatorStart(current))
                    tokens.Add(ParseOperatorToken());
                else if (current == '(')
                {
                    tokens.Add(new Token(TokenType.LeftParenthesis, "("));
                    Advance();
                }
                else if (current == ')')
                {
                    tokens.Add(new Token(TokenType.RightParenthesis, ")"));
                    Advance();
                }
                else if (current == ',')
                {
                    tokens.Add(new Token(TokenType.Comma, ","));
                    Advance();
                }
                else if (Char.IsLetter(current) || current == '_')
                {
                    tokens.Add(ParseIdentifierToken());
                }
                else
                {
                    // For unrecognized characters, add unknown token.
                    tokens.Add(new Token(TokenType.Unknown, current.ToString()));
                    Advance();
                }
            }

            return tokens;
        }

        private bool IsAtEnd() => _position >= _input.Length;

        private char Peek() => _input[_position];

        private char Advance() => _input[_position++];

        private void SkipWhitespace()
        {
            while (!IsAtEnd() && Char.IsWhiteSpace(Peek()))
                Advance();
        }

        private bool LookaheadIsDigit()
        {
            if (_position + 1 < _input.Length)
                return Char.IsDigit(_input[_position + 1]);
            return false;
        }

        // Checks if a character can start an operator.
        private bool IsOperatorStart(char c)
        {
            return "+-*/=!<>|&".IndexOf(c) >= 0;
        }

        // Determines valid continuation for an operator.
        private bool IsOperatorPart(char c)
        {
            return "=!<>&|".IndexOf(c) >= 0;
        }

        // Helper to determine if string is a valid operator.
        private TokenType? LookupOperatorTokenType(string op)
        {
            switch (op)
            {
                case "+": return TokenType.Plus;
                case "-": return TokenType.Minus;
                case "*": return TokenType.Multiply;
                case "/": return TokenType.Divide;
                case "==": return TokenType.Equal;
                case "!=":
                case "<>": return TokenType.NotEqual;
                case ">": return TokenType.GreaterThan;
                case "<": return TokenType.LessThan;
                case ">=": return TokenType.GreaterEqual;
                case "<=": return TokenType.LessEqual;
                case "&&":
                case "AND": return TokenType.And;
                case "||":
                case "OR": return TokenType.Or;
                case "!":
                case "NOT": return TokenType.Not;
                default: return null;
            }
        }

        // Parses an entity field token that starts with ':'.
        private Token ParseEntityFieldToken()
        {
            Advance(); // consume ':'
            StringBuilder sb = new StringBuilder();
            while (!IsAtEnd() && !Char.IsWhiteSpace(Peek()) && !IsSpecialDelimiter(Peek()))
            {
                sb.Append(Advance());
            }
            return new Token(TokenType.EntityField, sb.ToString());
        }

        // Parses a rule reference token that starts with '@'.
        private Token ParseRuleReferenceToken()
        {
            Advance(); // consume '@'
            StringBuilder sb = new StringBuilder();
            while (!IsAtEnd() && !Char.IsWhiteSpace(Peek()) && !IsSpecialDelimiter(Peek()))
            {
                sb.Append(Advance());
            }
            return new Token(TokenType.RuleReference, sb.ToString());
        }

        // Parses a literal starting with '#' (treated here as string literal).
        private Token ParseHashLiteralToken()
        {
            Advance(); // consume '#'
            StringBuilder sb = new StringBuilder();
            while (!IsAtEnd() && !Char.IsWhiteSpace(Peek()) && !IsSpecialDelimiter(Peek()))
            {
                sb.Append(Advance());
            }
            // You can choose to interpret this token further if needed.
            return new Token(TokenType.StringLiteral, sb.ToString());
        }

        // Parses a string literal enclosed in quotes.
        private Token ParseStringLiteralToken()
        {
            char quote = Advance(); // consume the starting quote (either " or ')
            StringBuilder sb = new StringBuilder();
            while (!IsAtEnd() && Peek() != quote)
            {
                if (Peek() == '\\') // Handle escaping
                {
                    Advance(); // consume the backslash
                    if (!IsAtEnd())
                        sb.Append(Advance());
                }
                else
                {
                    sb.Append(Advance());
                }
            }
            if (!IsAtEnd())
                Advance(); // consume the closing quote
            return new Token(TokenType.StringLiteral, sb.ToString());
        }

        // Parses a numeric literal (integer or decimal).
        private Token ParseNumericLiteralToken()
        {
            StringBuilder sb = new StringBuilder();
            bool hasDecimalPoint = false;

            while (!IsAtEnd())
            {
                char current = Peek();
                if (Char.IsDigit(current))
                {
                    sb.Append(Advance());
                }
                else if (current == '.' && !hasDecimalPoint)
                {
                    hasDecimalPoint = true;
                    sb.Append(Advance());
                }
                else
                {
                    break;
                }
            }

            // Optionally, you can try to parse it using Decimal.Parse(sb.ToString(), ...);
            return new Token(TokenType.NumericLiteral, sb.ToString());
        }

        // Parses an operator token (handles multi-character operators).
        private Token ParseOperatorToken()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Advance()); // consume first operator char
                                  // Continue adding operator characters if they are valid parts.
            while (!IsAtEnd() && IsOperatorPart(Peek()))
            {
                sb.Append(Advance());
            }
            string opCandidate = sb.ToString();
            // Try to find the longest matching valid operator.
            while (!string.IsNullOrEmpty(opCandidate) && LookupOperatorTokenType(opCandidate) == null)
            {
                // Remove last character until a valid operator is found.
                opCandidate = opCandidate.Substring(0, opCandidate.Length - 1);
                // Roll back the tokenizer position for the removed characters.
                _position--;
            }
            TokenType opType = LookupOperatorTokenType(opCandidate) ?? TokenType.Unknown;
            return new Token(opType, opCandidate);
        }

        // Parses an identifier token (function names or unprefixed variables).
        private Token ParseIdentifierToken()
        {
            StringBuilder sb = new StringBuilder();
            while (!IsAtEnd() && (Char.IsLetterOrDigit(Peek()) || Peek() == '_'))
            {
                sb.Append(Advance());
            }
            return new Token(TokenType.Identifier, sb.ToString());
        }

        // Determine if a character is a delimiter for tokens.
        private bool IsSpecialDelimiter(char c)
        {
            return c == ':' || c == '@' || c == '#' ||
                   c == '(' || c == ')' || c == ',' ||
                   IsOperatorStart(c);
        }
    }
}

