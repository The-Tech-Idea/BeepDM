namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>
    /// Query operators for building block-level query filters.
    /// Used by IQueryBuilderManager to map field-level operators.
    /// </summary>
    public enum QueryOperator
    {
        /// <summary>Equality comparison.</summary>
        Equals,

        /// <summary>Inequality comparison.</summary>
        NotEquals,

        /// <summary>Greater-than comparison.</summary>
        GreaterThan,

        /// <summary>Less-than comparison.</summary>
        LessThan,

        /// <summary>Greater-than-or-equal comparison.</summary>
        GreaterThanOrEqual,

        /// <summary>Less-than-or-equal comparison.</summary>
        LessThanOrEqual,

        /// <summary>Pattern comparison using LIKE semantics.</summary>
        Like,

        /// <summary>Negated pattern comparison using LIKE semantics.</summary>
        NotLike,

        /// <summary>Membership comparison against a set of values.</summary>
        In,

        /// <summary>Negated membership comparison against a set of values.</summary>
        NotIn,

        /// <summary>Inclusive range comparison.</summary>
        Between,

        /// <summary>Null test.</summary>
        IsNull,

        /// <summary>Non-null test.</summary>
        IsNotNull,

        /// <summary>Prefix comparison.</summary>
        StartsWith,

        /// <summary>Suffix comparison.</summary>
        EndsWith,

        /// <summary>Substring comparison.</summary>
        Contains
    }
}
