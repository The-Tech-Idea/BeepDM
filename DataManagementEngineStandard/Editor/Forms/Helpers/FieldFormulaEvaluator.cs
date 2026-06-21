using System;
using System.Collections.Generic;
using System.Globalization;

namespace TheTechIdea.Beep.Editor.Forms.Helpers;

/// <summary>
/// Infix formula evaluator for Oracle Forms <c>Calculation = Formula</c>.
/// Evaluates numeric expressions with field references resolved against a record dictionary.
/// Supports +, -, *, / with standard precedence and parentheses.
/// Shared between WPF and WinForms UI layers.
/// </summary>
public sealed class FieldFormulaEvaluator
{
    private readonly string _expression;
    private readonly IDictionary<string, object?> _record;
    private int _pos;

    public FieldFormulaEvaluator(string expression, IDictionary<string, object?> record)
    {
        _expression = expression ?? string.Empty;
        _record = record ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    public double Evaluate()
    {
        _pos = 0;
        double result = ParseExpression();
        SkipWhitespace();
        if (_pos < _expression.Length)
            throw new FormatException($"Unexpected character '{_expression[_pos]}' at position {_pos}.");
        return result;
    }

    private double ParseExpression()
    {
        double left = ParseTerm();
        while (true)
        {
            SkipWhitespace();
            if (_pos >= _expression.Length) return left;
            char op = _expression[_pos];
            if (op != '+' && op != '-') return left;
            _pos++;
            double right = ParseTerm();
            left = op == '+' ? left + right : left - right;
        }
    }

    private double ParseTerm()
    {
        double left = ParseFactor();
        while (true)
        {
            SkipWhitespace();
            if (_pos >= _expression.Length) return left;
            char op = _expression[_pos];
            if (op != '*' && op != '/') return left;
            _pos++;
            double right = ParseFactor();
            left = op == '*' ? left * right : (right == 0 ? 0 : left / right);
        }
    }

    private double ParseFactor()
    {
        SkipWhitespace();
        if (_pos >= _expression.Length)
            throw new FormatException("Unexpected end of expression.");

        if (_expression[_pos] == '(')
        {
            _pos++;
            double inner = ParseExpression();
            SkipWhitespace();
            if (_pos >= _expression.Length || _expression[_pos] != ')')
                throw new FormatException("Missing closing parenthesis.");
            _pos++;
            return inner;
        }

        if (char.IsDigit(_expression[_pos]) || _expression[_pos] == '.')
            return ParseNumber();

        int start = _pos;
        while (_pos < _expression.Length && !"+-*/()".Contains(_expression[_pos]))
            _pos++;
        string name = _expression.Substring(start, _pos - start).Trim();
        if (string.IsNullOrEmpty(name))
            throw new FormatException("Empty field reference.");
        if (_record.TryGetValue(name, out var raw) && raw != null
            && double.TryParse(raw.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            return d;
        return 0d;
    }

    private double ParseNumber()
    {
        int start = _pos;
        while (_pos < _expression.Length && (char.IsDigit(_expression[_pos]) || _expression[_pos] == '.'))
            _pos++;
        string text = _expression.Substring(start, _pos - start);
        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            return d;
        throw new FormatException($"Invalid number '{text}'.");
    }

    private void SkipWhitespace()
    {
        while (_pos < _expression.Length && char.IsWhiteSpace(_expression[_pos]))
            _pos++;
    }
}
