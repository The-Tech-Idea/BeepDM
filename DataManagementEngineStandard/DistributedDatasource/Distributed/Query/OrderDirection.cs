namespace TheTechIdea.Beep.Distributed.Query
{
    /// <summary>
    /// Sort direction for a single <see cref="OrderBySpec"/> column.
    /// </summary>
    public enum OrderDirection
    {
        /// <summary><c>ASC</c> — smaller values first. The default.</summary>
        Ascending = 0,

        /// <summary><c>DESC</c> — larger values first.</summary>
        Descending = 1,
    }
}
