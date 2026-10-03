using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.Defaults.Helpers;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    // Required queries validate every filter before context getters or provider callbacks.
    internal static class RequiredDataSourceRule
    {
        private sealed record Filter(string Field, string Operator, string Value, bool Bind);
        private sealed record Plan(string Mode, string Entity, string Field, List<Filter> Filters);

        private static void Deny() => throw new ImportTransformationException(ImportTransformationStage.Defaults);
        private static void Check()
        {
            RequiredDefaultResolution.Current?.Token.ThrowIfCancellationRequested();
            if (RequiredDefaultResolution.Current?.Failed == true) Deny();
        }

        internal static object Resolve(string rule, IDMEEditor editor, IPassedArgs parameters)
        {
            var plan = Parse(rule);
            var filters = new List<AppFilter>();
            long bytes = 0;
            foreach (var filter in plan.Filters)
            {
                Check();
                var text = filter.Bind ? Bind(filter.Value, parameters) : filter.Value;
                if (text.Length > 1048576 || (bytes += 2L * text.Length) > 16 * 1024 * 1024) Deny();
                filters.Add(new AppFilter { FieldName = filter.Field, Operator = filter.Operator, FilterValue = text });
            }

            Check();
            var source = parameters?.DataSource;
            Check();
            if (source == null)
            {
                var name = parameters?.DatasourceName;
                Check();
                if (string.IsNullOrWhiteSpace(name)) Deny();
                source = editor.GetDataSource(name);
                Check();
                if (source == null) Deny();
            }
            var rows = source.GetEntity(plan.Entity, filters);
            Status(source);
            if (rows == null) Deny();

            var visited = 0;
            var values = 0;
            object result = null;
            object total = 0m;
            using (var iterator = rows.GetEnumerator())
            {
                Check();
                if (iterator == null) Deny();
                while (true)
                {
                    Check();
                    var moved = iterator.MoveNext();
                    Status(source);
                    if (!moved) break;
                    if (++visited > 100000) Deny();
                    var row = iterator.Current;
                    Status(source);
                    if (row == null) Deny();
                    if (plan.Mode == "FIRST") { result = row; break; }
                    if (plan.Mode == "EXISTS") { result = true; break; }
                    if (plan.Mode == "COUNT") continue;

                    var value = RecordFieldAccess.ReadRequired(row, plan.Field);
                    Status(source);
                    value = DefaultValueHelper.CopyLiteralRequired(value);
                    if (plan.Mode == "SCALAR") { result = value; break; }
                    // Real null fields are omitted by aggregate semantics; missing/bad fields are never omitted.
                    if (value == null) continue;
                    values++;
                    if (plan.Mode is "SUM" or "AVG")
                    {
                        if (!RequiredExpressionEvaluator.IsNumber(value)) Deny();
                        total = RequiredExpressionEvaluator.Arithmetic("+", total, value);
                    }
                    else
                    {
                        if (!IsOrdered(value)) Deny();
                        if (result == null || RequiredExpressionEvaluator.Compare(plan.Mode == "MIN" ? "<" : ">", value, result))
                            result = value;
                    }
                }
            }
            Status(source);
            if (plan.Mode == "COUNT") return visited;
            if (plan.Mode == "EXISTS") return visited != 0;
            if (plan.Mode is "SUM" or "AVG")
                return values == 0 ? null : RequiredExpressionEvaluator.ToDouble(plan.Mode == "SUM" ? total :
                    RequiredExpressionEvaluator.Arithmetic("/", total, values));
            return result;
        }

        private static void Status(IDataSource source)
        {
            Check();
            var errors = source.ErrorObject;
            Check();
            if (errors != null)
            {
                var flag = errors.Flag;
                Check();
                if (flag != Errors.Ok) Deny();
                var exception = errors.Ex;
                Check();
                if (exception != null) Deny();
            }
        }

        private static bool IsOrdered(object value) => RequiredExpressionEvaluator.IsNumber(value) ||
            value is string or char or DateTime or DateTimeOffset or DateOnly or TimeOnly or TimeSpan;

        private static Plan Parse(string rule)
        {
            Check();
            var text = rule.Trim();
            var open = text.IndexOf('(');
            if (open <= 0 || !text.EndsWith(")", StringComparison.Ordinal) || !RequiredDefaultResolution.HasValidEnvelope(text)) Deny();
            var op = text.Substring(0, open).ToUpperInvariant();
            var args = RequiredBuiltInRule.SplitArguments(text.Substring(open + 1, text.Length - open - 2));
            if (args.Length > 260) Deny();
            string Atom(int index)
            {
                if (index >= args.Length) Deny();
                return RequiredBuiltInRule.RequireAtom(args[index], nonempty: true);
            }
            var mode = op;
            var entityIndex = 0;
            var fieldIndex = 1;
            if (op is "GETENTITY") mode = "FIRST";
            else if (op is "LOOKUP" or "ENTITYLOOKUP") mode = "SCALAR";
            else if (op == "QUERY")
            {
                mode = Atom(0).ToUpperInvariant();
                entityIndex = 1; fieldIndex = 2;
                if (mode == "AGGREGATE")
                {
                    mode = Atom(1).ToUpperInvariant();
                    if (mode is not ("MIN" or "MAX" or "SUM" or "AVG")) Deny();
                    entityIndex = 2; fieldIndex = 3;
                }
                else if (mode is not ("SCALAR" or "FIRST" or "EXISTS" or "COUNT")) Deny();
            }
            else if (op is not ("COUNT" or "MIN" or "MAX" or "SUM" or "AVG")) Deny();

            var entity = Atom(entityIndex);
            var field = mode is "SCALAR" or "MIN" or "MAX" or "SUM" or "AVG" ? Atom(fieldIndex) : null;
            var filterIndex = field == null ? entityIndex + 1 : fieldIndex + 1;
            if (args.Length - filterIndex > 256) Deny();
            var filters = new List<Filter>();
            for (var i = filterIndex; i < args.Length; i++) { Check(); filters.Add(ParseFilter(args[i])); }
            return new Plan(mode, entity, field, filters);
        }

        private static Filter ParseFilter(string condition)
        {
            if (RequiredBuiltInRule.IsQuotedLiteral(condition)) condition = condition.Substring(1, condition.Length - 2);
            var index = -1;
            var length = 0;
            var quote = '\0';
            for (var i = 0; i < condition.Length; i++)
            {
                var ch = condition[i];
                if (quote != '\0') { if (ch == quote) quote = '\0'; continue; }
                if (ch is '\'' or '"') { quote = ch; continue; }
                if (ch is not ('=' or '!' or '<' or '>')) continue;
                if (index >= 0) Deny();
                index = i; length = 1;
                if (i + 1 < condition.Length && condition[i + 1] == '=' && ch != '=') { length = 2; i++; }
                else if (ch == '!') Deny();
            }
            if (quote != '\0' || index <= 0) Deny();
            var field = RequiredBuiltInRule.RequireAtom(condition.Substring(0, index), nonempty: true);
            var raw = condition.Substring(index + length).Trim();
            if (raw.Length == 0) Deny();
            var quoted = RequiredBuiltInRule.IsQuotedLiteral(raw);
            var value = RequiredBuiltInRule.RequireAtom(raw, nonempty: false);
            if (!quoted && value.Any(ch => char.IsWhiteSpace(ch) || ch is '=' or '!' or '<' or '>' or '&' or '|' or ';')) Deny();
            var bind = !quoted && value[0] == '@';
            if (bind)
            {
                value = value.Substring(1);
                if (value.Length == 0 || !(char.IsLetter(value[0]) || value[0] == '_') ||
                    value.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '_' or '.')) ||
                    value.EndsWith(".", StringComparison.Ordinal) || value.Contains("..")) Deny();
            }
            return new Filter(field, condition.Substring(index, length), value, bind);
        }

        private static string Bind(string name, IPassedArgs parameters)
        {
            Check();
            if (parameters == null) Deny();
            var objects = parameters.Objects;
            Check();
            if (objects?.Count > 10000) Deny();
            var named = objects?.Where(item => item != null && string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
            Check();
            if (named?.Length > 1) Deny();
            if (named?.Length == 1) return FilterText(named[0].obj);
            if (name.Equals("FieldName", StringComparison.OrdinalIgnoreCase))
            {
                var field = parameters.ParameterString1;
                Check();
                if (field != null) return FilterText(field);
            }
            var record = parameters.ReturnData;
            Check();
            if (record == null)
            {
                var records = objects?.Where(item => item != null && string.Equals(item.Name, "Record", StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
                Check();
                if (records?.Length != 1) Deny();
                record = records[0].obj;
            }
            var value = RecordFieldAccess.ReadRequired(record, name);
            Check();
            return FilterText(value);
        }

        private static string FilterText(object value)
        {
            Check();
            if (value == null || value == DBNull.Value) Deny();
            value = DefaultValueHelper.CopyLiteralRequired(value);
            if (value is byte[]) Deny();
            return value switch
            {
                string text => text,
                bool boolean => boolean ? "true" : "false",
                char character => character.ToString(),
                float number => number.ToString("R", CultureInfo.InvariantCulture),
                double number => number.ToString("R", CultureInfo.InvariantCulture),
                decimal number => number.ToString("G29", CultureInfo.InvariantCulture),
                DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
                DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture),
                DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                TimeOnly time => time.ToString("O", CultureInfo.InvariantCulture),
                TimeSpan time => time.ToString("c", CultureInfo.InvariantCulture),
                Guid guid => guid.ToString("D"),
                _ when RequiredExpressionEvaluator.IsNumber(value) => Convert.ToString(value, CultureInfo.InvariantCulture),
                _ => throw new ImportTransformationException(ImportTransformationStage.Defaults)
            };
        }
    }
}
