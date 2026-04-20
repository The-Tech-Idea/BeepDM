using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Distributed.Query
{
    /// <summary>
    /// Folds per-shard partial aggregate values into a running total
    /// for one <see cref="AggregateKind"/>. Used by the Phase 08
    /// <see cref="QueryAwareResultMerger"/> while walking grouped
    /// rows from every shard.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The accumulator is deliberately untyped — partial values may
    /// arrive as <c>int</c>, <c>long</c>, <c>decimal</c>, <c>double</c>,
    /// <see cref="DateTime"/>, or <see cref="string"/>. SUM / COUNT
    /// are folded in <see cref="decimal"/> when the inputs are
    /// integral/decimal and in <see cref="double"/> otherwise.
    /// MIN / MAX use <see cref="Comparer{T}.Default"/>-style
    /// comparisons via <see cref="CompareValues"/>.
    /// </para>
    /// <para>
    /// <see cref="AggregateKind.Avg"/> is not handled here directly;
    /// the planner splits AVG into a SUM+COUNT pair and the merger
    /// re-pairs them after all accumulators finish.
    /// </para>
    /// </remarks>
    public sealed class AggregateAccumulator
    {
        private readonly AggregateKind _kind;
        private bool      _hasValue;
        private bool      _useDouble;
        private decimal   _decimalTotal;
        private double    _doubleTotal;
        private long      _countTotal;
        private object    _extremum;

        /// <summary>Creates a new accumulator.</summary>
        /// <param name="kind">Aggregate kind; <see cref="AggregateKind.Avg"/> is rejected.</param>
        public AggregateAccumulator(AggregateKind kind)
        {
            if (kind == AggregateKind.Avg)
            {
                throw new ArgumentException(
                    "AVG must be split into SUM/COUNT by the planner; AggregateAccumulator does not handle AVG directly.",
                    nameof(kind));
            }

            _kind = kind;
        }

        /// <summary>Aggregate kind being folded.</summary>
        public AggregateKind Kind => _kind;

        /// <summary><c>true</c> once at least one value has been folded.</summary>
        public bool HasValue => _hasValue;

        /// <summary>
        /// Folds <paramref name="value"/> into the running total.
        /// <c>null</c> values are ignored so missing shard
        /// contributions behave like <c>SUM</c> of nothing (i.e.
        /// zero) rather than poisoning the total.
        /// </summary>
        public void Add(object value)
        {
            if (value == null || value is DBNull) return;

            switch (_kind)
            {
                case AggregateKind.Count:
                    _countTotal += ToLongOrOne(value);
                    _hasValue    = true;
                    break;

                case AggregateKind.Sum:
                    FoldNumeric(value);
                    _hasValue = true;
                    break;

                case AggregateKind.Min:
                    if (!_hasValue || CompareValues(value, _extremum) < 0)
                    {
                        _extremum = value;
                        _hasValue = true;
                    }
                    break;

                case AggregateKind.Max:
                    if (!_hasValue || CompareValues(value, _extremum) > 0)
                    {
                        _extremum = value;
                        _hasValue = true;
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported aggregate kind '{_kind}'.");
            }
        }

        /// <summary>
        /// Returns the folded result — or <c>null</c> when no values
        /// were observed (mirrors ANSI SQL semantics for an empty
        /// aggregate over <c>SUM</c> / <c>MIN</c> / <c>MAX</c>;
        /// <c>COUNT</c> returns <c>0L</c>).
        /// </summary>
        public object GetResult()
        {
            switch (_kind)
            {
                case AggregateKind.Count:
                    return _countTotal;

                case AggregateKind.Sum:
                    if (!_hasValue) return null;
                    return _useDouble ? (object)_doubleTotal : _decimalTotal;

                case AggregateKind.Min:
                case AggregateKind.Max:
                    return _hasValue ? _extremum : null;

                default:
                    throw new InvalidOperationException($"Unsupported aggregate kind '{_kind}'.");
            }
        }

        /// <summary>
        /// Divides <paramref name="sum"/> by <paramref name="count"/>
        /// to produce an AVG value. Falls back to <c>double</c> math
        /// when either operand is a floating-point value.
        /// </summary>
        /// <param name="sum">Folded SUM value (may be <c>null</c>).</param>
        /// <param name="count">Folded COUNT value.</param>
        /// <returns>The average, or <c>null</c> when count == 0 or sum is <c>null</c>.</returns>
        public static object DivideAverage(object sum, object count)
        {
            if (sum == null) return null;

            long countLong = ToLongOrOne(count);
            if (countLong == 0) return null;

            if (sum is double ds)
            {
                return ds / countLong;
            }

            if (sum is float fs)
            {
                return (double)fs / countLong;
            }

            if (sum is decimal decSum)
            {
                return decSum / countLong;
            }

            // Fallback: coerce to decimal when possible.
            try
            {
                var d = Convert.ToDecimal(sum);
                return d / countLong;
            }
            catch (Exception)
            {
                return Convert.ToDouble(sum) / countLong;
            }
        }

        // ── Folding helpers ───────────────────────────────────────────────

        private void FoldNumeric(object value)
        {
            if (!_useDouble && TryToDecimal(value, out var dec))
            {
                _decimalTotal += dec;
                return;
            }

            // Switch to double-mode if we encounter a float/double; port the running sum.
            if (!_useDouble)
            {
                _doubleTotal = (double)_decimalTotal;
                _useDouble   = true;
            }

            _doubleTotal += ToDouble(value);
        }

        private static bool TryToDecimal(object value, out decimal result)
        {
            try
            {
                switch (value)
                {
                    case byte b:    result = b;                 return true;
                    case sbyte sb:  result = sb;                return true;
                    case short s:   result = s;                 return true;
                    case ushort us: result = us;                return true;
                    case int i:     result = i;                 return true;
                    case uint ui:   result = ui;                return true;
                    case long l:    result = l;                 return true;
                    case ulong ul:  result = ul;                return true;
                    case decimal d: result = d;                 return true;
                    case double:
                    case float:
                        result = 0m;
                        return false;
                    case string str:
                        return decimal.TryParse(str, out result);
                    default:
                        result = Convert.ToDecimal(value);
                        return true;
                }
            }
            catch (Exception)
            {
                result = 0m;
                return false;
            }
        }

        private static double ToDouble(object value)
        {
            try { return Convert.ToDouble(value); }
            catch (Exception) { return 0d; }
        }

        private static long ToLongOrOne(object value)
        {
            if (value == null || value is DBNull) return 1L;

            try { return Convert.ToInt64(value); }
            catch (Exception) { return 1L; }
        }

        private static int CompareValues(object left, object right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left  == null) return -1;
            if (right == null) return  1;

            if (left is IComparable lc && left.GetType() == right.GetType())
            {
                return lc.CompareTo(right);
            }

            // Numeric cross-type compare: normalise to double.
            if (IsNumeric(left) && IsNumeric(right))
            {
                return ToDouble(left).CompareTo(ToDouble(right));
            }

            return string.Compare(
                Convert.ToString(left),
                Convert.ToString(right),
                StringComparison.Ordinal);
        }

        private static bool IsNumeric(object value)
            => value is byte || value is sbyte
            || value is short || value is ushort
            || value is int   || value is uint
            || value is long  || value is ulong
            || value is float || value is double
            || value is decimal;
    }
}
