namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Optional explicit capability for inclusive civil-date guards using the provider UTC day.
    /// Implementations enforce the entire alternative group at mutation and immediately before commit.
    /// Older conditional providers without this capability must be refused before dispatch.</summary>
    public interface ICalendarGuardedMutationDataSource
    {
        /// <summary>Whether this configured provider implements the calendar guard contract.</summary>
        bool SupportsCalendarGuards { get; }
    }
}
