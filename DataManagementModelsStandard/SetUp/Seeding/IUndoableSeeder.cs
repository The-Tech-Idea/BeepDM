using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.SetUp.Seeding
{
    /// <summary>
    /// A seeder that can remove the data it inserted, so <c>SeedingStep</c> can participate in a
    /// rollback. Seeders that don't implement this are reported as <em>skipped</em> during rollback
    /// (nothing undone), never as a clean undo that didn't happen.
    /// </summary>
    public interface IUndoableSeeder : ISeeder
    {
        /// <summary>
        /// Removes the rows this seeder inserted. Must not throw; return <c>Errors.Ok</c> on success.
        /// </summary>
        IErrorsInfo Unseed(IDataSource dataSource, IDMEEditor editor,
            System.IProgress<PassedArgs> progress = null);
    }
}
