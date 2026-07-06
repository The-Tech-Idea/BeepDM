using System;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Published by the seeding UI step when its seeder-registry summary changes.
    /// </summary>
    public sealed class SeedingSummaryEventArgs : EventArgs
    {
        public SeedingSummaryEventArgs(int totalSeeders, int orderedSeeders, string message)
        {
            TotalSeeders = totalSeeders;
            OrderedSeeders = orderedSeeders;
            Message = message;
        }

        public int TotalSeeders { get; }
        public int OrderedSeeders { get; }
        public string Message { get; }
    }
}
