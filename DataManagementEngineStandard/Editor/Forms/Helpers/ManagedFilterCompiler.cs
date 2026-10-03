using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Extensions;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>Closed, bounded AND-filter compilation for mandatory managed read restrictions.</summary>
    internal sealed class ManagedFilterCompiler
    {
        private const int MaxFilters = 128;
        private readonly Dictionary<string, Type> _fields;
        private readonly List<AppFilter> _filters = new();
        private int _totalCharacters;

        internal ManagedFilterCompiler(IEnumerable<EntityField> fields)
        {
            _fields = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in fields ?? Enumerable.Empty<EntityField>())
            {
                if (string.IsNullOrWhiteSpace(field.FieldName)) continue;
                if (!_fields.TryAdd(field.FieldName, ResolveType(field.Fieldtype)))
                    throw new InvalidOperationException("Duplicate field identities in managed query metadata.");
            }
        }

        internal List<AppFilter> Compile(IEnumerable<AppFilter> caller, string defaultClause,
            string policyClause, IReadOnlyDictionary<string, object> parameters)
        {
            foreach (var filter in caller ?? Enumerable.Empty<AppFilter>())
            {
                if (filter == null) throw new FormatException("Null filter in managed query.");
                if (string.Equals(filter.Operator, "OrderBy", StringComparison.OrdinalIgnoreCase))
                {
                    FieldType(filter.FieldName);
                    if (filter.FilterValue != "ASC" && filter.FilterValue != "DESC") throw new FormatException("Invalid sort direction.");
                    Add(new AppFilter { FieldName = filter.FieldName, Operator = "OrderBy", FilterValue = filter.FilterValue });
                    continue;
                }
                var normalized = DataSourceAppFilterExtensions.NormalizeFilters(null, new[] { filter });
                if (normalized.Count != 1) throw new FormatException("Unsupported managed query operator.");
                var copy = normalized[0];
                var values = copy.Operator == "is null" || copy.Operator == "is not null" ? Array.Empty<object>() :
                    copy.Operator.Contains("between", StringComparison.Ordinal)
                    ? new object[] { copy.FilterValue, copy.FilterValue1 }
                    : copy.Operator.Contains("in", StringComparison.Ordinal) && (copy.Operator == "in" || copy.Operator == "not in")
                        ? DataSourceAppFilterExtensions.ParseCollectionFilterValues(copy.FilterValue).Cast<object>().ToArray()
                        : new object[] { copy.FilterValue };
                AddPredicate(copy.FieldName, copy.Operator, values);
            }
            ParseClause(defaultClause, parameters ?? new Dictionary<string, object>());
            ParseClause(policyClause, parameters ?? new Dictionary<string, object>());
            return _filters;
        }

        private void ParseClause(string clause, IReadOnlyDictionary<string, object> parameters)
        {
            if (string.IsNullOrWhiteSpace(clause)) return;
            if (clause.Length > 16384) throw new FormatException("Managed WHERE clause exceeds its limit.");
            var parser = new ClauseParser(this, clause, parameters);
            parser.Parse();
        }

        internal Type FieldType(string name)
        {
            if (name == null || !_fields.TryGetValue(name, out var type))
                throw new FormatException("Unknown field in managed query.");
            if (type == null) throw new FormatException("Unsupported field type in managed query.");
            return type;
        }

        private void Add(AppFilter filter)
        {
            if (_filters.Count >= MaxFilters) throw new FormatException("Managed filter count exceeds its limit.");
            _totalCharacters = checked(_totalCharacters + (filter.FieldName?.Length ?? 0) +
                (filter.FilterValue?.Length ?? 0) + (filter.FilterValue1?.Length ?? 0));
            if (_totalCharacters > 1048576) throw new FormatException("Managed filter payload exceeds its limit.");
            _filters.Add(filter);
        }

        private void AddPredicate(string field, string op, object[] values)
        {
            var type = FieldType(field);
            var serialized = values.Select(value => SerializeValue(value, type)).ToArray();
            if (op == "=" || op == "!=")
            {
                if (serialized.Length != 1) throw new FormatException("Invalid comparison arity.");
                if (serialized[0] == null) op = op == "=" ? "is null" : "is not null";
            }
            if (op != "is null" && op != "is not null" && serialized.Any(v => v == null))
                throw new FormatException("Null requires an equality or IS NULL predicate.");
            if ((op == "like" || op == "not like" || op == "contains" || op == "startswith" || op == "endswith") && type != typeof(string))
                throw new FormatException("LIKE requires a string field.");
            if ((op == "in" || op == "not in") && (serialized.Length == 0 || serialized.Length > MaxFilters))
                throw new FormatException("Invalid managed IN value count.");
            Add(new AppFilter
            {
                FieldName = _fields.Keys.First(k => string.Equals(k, field, StringComparison.OrdinalIgnoreCase)),
                Operator = op, FieldType = type, valueType = type.FullName,
                FilterValue = op == "in" || op == "not in"
                    ? string.Join(",", serialized.Select(v => "'" + v.Replace("'", "''") + "'")) : serialized.FirstOrDefault(),
                FilterValue1 = serialized.Length == 2 ? serialized[1] : null
            });
        }

        internal static object TypedValue(string value, Type type)
        {
            if (value == null) return DBNull.Value;
            if (type == typeof(string)) return value;
            if (type == typeof(Guid)) return Guid.Parse(value);
            if (type == typeof(DateTime)) return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            if (type == typeof(DateTimeOffset)) return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            if (type == typeof(bool)) return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase) ? true :
                value == "0" || value.Equals("false", StringComparison.OrdinalIgnoreCase) ? false : throw new FormatException("Invalid Boolean filter value.");
            var parsed = type == typeof(decimal) ? (object)decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture) :
                type == typeof(double) ? double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture) :
                type == typeof(float) ? float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture) :
                Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            if (parsed is double d && !double.IsFinite(d) || parsed is float f && !float.IsFinite(f))
                throw new FormatException("Non-finite filter value.");
            return parsed;
        }

        private static string SerializeValue(object value, Type type)
        {
            if (value == null || value == DBNull.Value) return null;
            var suppliedType = value.GetType();
            if (ResolveType(suppliedType.FullName) == null && suppliedType != typeof(char))
                throw new FormatException("Unsupported managed filter value type.");
            var text = value switch
            {
                DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
                DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture),
                double number => number.ToString("R", CultureInfo.InvariantCulture),
                float number => number.ToString("R", CultureInfo.InvariantCulture),
                bool boolean => boolean ? "True" : "False",
                string str => str,
                char character => character.ToString(),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => throw new FormatException("Unsupported managed filter value.")
            };
            if (text.Length > 65536) throw new FormatException("Managed filter value exceeds its limit.");
            var parsed = TypedValue(text, type);
            return parsed switch
            {
                DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
                DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture),
                bool boolean => boolean ? "True" : "False",
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => (string)parsed
            };
        }

        internal static string FormatInputValue(object value)
        {
            if (value == null || value == DBNull.Value) return null;
            var type = value is char ? typeof(string) : ResolveType(value.GetType().FullName);
            if (type == null) throw new FormatException("Unsupported managed relationship key type.");
            return SerializeValue(value, type);
        }

        private static Type ResolveType(string name) => (name ?? "string").Replace("System.", "", StringComparison.OrdinalIgnoreCase)
            .Trim().TrimEnd('?').ToLowerInvariant() switch
        {
            "string" or "char" or "text" or "varchar" or "nvarchar" => typeof(string),
            "byte" => typeof(byte), "sbyte" => typeof(sbyte), "short" or "int16" => typeof(short),
            "ushort" or "uint16" => typeof(ushort), "int" or "int32" => typeof(int),
            "uint" or "uint32" => typeof(uint), "long" or "int64" => typeof(long),
            "ulong" or "uint64" => typeof(ulong), "decimal" => typeof(decimal),
            "double" => typeof(double), "float" or "single" => typeof(float),
            "bool" or "boolean" or "bit" => typeof(bool), "guid" => typeof(Guid),
            "datetime" or "date" => typeof(DateTime), "datetimeoffset" => typeof(DateTimeOffset), _ => null
        };

        private sealed class ClauseParser
        {
            private readonly ManagedFilterCompiler _owner;
            private readonly IReadOnlyDictionary<string, object> _parameters;
            private readonly string _clause;
            private int _position;
            private string _token;
            private char _kind;

            internal ClauseParser(ManagedFilterCompiler owner, string clause, IReadOnlyDictionary<string, object> parameters)
            { _owner = owner; _clause = clause; _parameters = parameters; Next(); }

            internal void Parse()
            {
                if (Is("WHERE")) Next();
                Conjunction(0);
                if (_kind != '\0') throw new FormatException("Unsupported or trailing managed WHERE syntax.");
            }

            private bool Is(string token) => _kind == 'w' && string.Equals(_token, token, StringComparison.OrdinalIgnoreCase);
            private void Conjunction(int depth)
            {
                if (depth > 32) throw new FormatException("Managed WHERE nesting exceeds its limit.");
                Term(depth);
                while (Is("AND")) { Next(); Term(depth); }
            }

            private void Term(int depth)
            {
                if (_kind == '(') { Next(); Conjunction(depth + 1); Require(')'); return; }
                if (_kind != 'w' && _kind != 'i') throw new FormatException("Expected a managed field identifier.");
                var field = _token; Next();
                string op;
                object[] values;
                if (Is("IS"))
                {
                    Next(); var negate = Is("NOT"); if (negate) Next();
                    if (!Is("NULL")) throw new FormatException("Expected NULL after IS.");
                    Next(); op = negate ? "is not null" : "is null"; values = Array.Empty<object>();
                }
                else
                {
                    var negate = Is("NOT"); if (negate) Next();
                    if (Is("IN"))
                    {
                        Next(); Require('('); var list = new List<object> { Value() };
                        while (_kind == ',') { Next(); list.Add(Value()); if (list.Count > MaxFilters) throw new FormatException("IN exceeds its limit."); }
                        Require(')'); op = negate ? "not in" : "in"; values = list.ToArray();
                    }
                    else if (Is("BETWEEN"))
                    {
                        Next(); var first = Value(); if (!Is("AND")) throw new FormatException("BETWEEN requires AND.");
                        Next(); values = new[] { first, Value() }; op = negate ? "not between" : "between";
                    }
                    else if (Is("LIKE")) { Next(); values = new[] { Value() }; op = negate ? "not like" : "like"; }
                    else
                    {
                        if (negate || _kind != 'o') throw new FormatException("Unsupported managed predicate.");
                        op = _token == "<>" ? "!=" : _token; Next(); values = new[] { Value() };
                    }
                }
                _owner.AddPredicate(field, op, values);
            }

            private object Value()
            {
                object value;
                if (_kind == 'p')
                {
                    if (!_parameters.TryGetValue(_token, out value)) throw new FormatException("Unresolved managed WHERE parameter.");
                }
                else if (_kind == 's' || _kind == 'n') value = _token;
                else if (Is("NULL")) value = null;
                else if (Is("TRUE")) value = true;
                else if (Is("FALSE")) value = false;
                else throw new FormatException("Expected a literal or named parameter.");
                Next(); return value;
            }

            private void Require(char kind)
            {
                if (_kind != kind) throw new FormatException("Unbalanced managed WHERE syntax.");
                Next();
            }

            private void Next()
            {
                while (_position < _clause.Length && char.IsWhiteSpace(_clause[_position])) _position++;
                if (_position == _clause.Length) { _kind = '\0'; _token = null; return; }
                var ch = _clause[_position++];
                if (ch == '(' || ch == ')' || ch == ',') { _kind = ch; _token = ch.ToString(); return; }
                if (ch == '\'' || ch == '"' || ch == '[' || ch == '`')
                {
                    var end = ch == '[' ? ']' : ch; var text = new StringBuilder();
                    while (_position < _clause.Length)
                    {
                        var item = _clause[_position++];
                        if (item != end) { text.Append(item); continue; }
                        if (_position < _clause.Length && _clause[_position] == end) { text.Append(end); _position++; continue; }
                        _kind = ch == '\'' ? 's' : 'i'; _token = text.ToString(); return;
                    }
                    throw new FormatException("Unterminated managed WHERE quote.");
                }
                if (ch == '=' || ch == '<' || ch == '>' || ch == '!')
                {
                    _token = ch.ToString();
                    if (_position < _clause.Length && (_clause[_position] == '=' || ch == '<' && _clause[_position] == '>')) _token += _clause[_position++];
                    if (_token != "=" && _token != "!=" && _token != "<>" && _token != "<" && _token != ">" && _token != "<=" && _token != ">=")
                        throw new FormatException("Invalid managed comparison.");
                    _kind = 'o'; return;
                }
                var start = _position - 1;
                if (ch == ':') { start = _position; _kind = 'p'; }
                else _kind = char.IsDigit(ch) || ch == '+' || ch == '-' ? 'n' : 'w';
                if (_kind == 'w' && !char.IsLetter(ch) && ch != '_') throw new FormatException("Invalid managed WHERE token.");
                while (_position < _clause.Length && (char.IsLetterOrDigit(_clause[_position]) || _clause[_position] == '_' ||
                    _kind == 'n' && (_clause[_position] == '.' || _clause[_position] == '+' || _clause[_position] == '-'))) _position++;
                _token = _clause.Substring(start, _position - start);
                if (_token.Length == 0) throw new FormatException("Empty managed parameter.");
                if (_kind == 'n' && (!double.TryParse(_token, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || !double.IsFinite(number)))
                    throw new FormatException("Invalid managed numeric literal.");
            }
        }
    }
}
