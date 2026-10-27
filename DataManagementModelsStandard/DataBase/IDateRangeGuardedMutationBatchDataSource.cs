namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Optional explicit admission for physical DateTime cohort intervals in conditional batches.
    /// Older batch producers must be refused rather than silently dropping interval predicates.</summary>
    public interface IDateRangeGuardedMutationBatchDataSource
    {
        /// <summary>Whether the configured native provider enforces all date-range cohort observations.</summary>
        bool SupportsDateRangeGuards { get; }
    }
}
