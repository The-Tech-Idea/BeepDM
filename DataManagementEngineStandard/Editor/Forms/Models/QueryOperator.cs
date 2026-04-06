namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>
    /// Query operators for building block-level query filters.
    /// Used by IQueryBuilderManager to map field-level operators.
    /// </summary>
    public enum QueryOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        Like,
        NotLike,
        In,
        NotIn,
        Between,
        IsNull,
        IsNotNull,
        StartsWith,
        EndsWith,
        Contains
    }
}
