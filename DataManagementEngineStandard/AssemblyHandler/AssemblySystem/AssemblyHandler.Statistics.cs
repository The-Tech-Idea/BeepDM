using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// AssemblyHandler partial class - Load Statistics
    /// Tracks assembly loading performance and outcomes.
    /// </summary>
    public partial class AssemblyHandler
    {
        #region Statistics Fields

        private AssemblyLoadStatistics _loadStatistics = new AssemblyLoadStatistics();
        private Stopwatch _loadStopwatch;

        #endregion

        #region Statistics Methods

        /// <summary>
        /// Gets the current load statistics.
        /// </summary>
        public AssemblyLoadStatistics GetLoadStatistics()
        {
            // Refresh dynamic counts from current state
            _loadStatistics.TotalAssembliesLoaded = LoadedAssemblies?.Count ?? 0;
            _loadStatistics.DriversFound = DataDriversConfig?.Count ?? 0;
            _loadStatistics.DataSourcesFound = DataSourcesClasses?.Count ?? 0;

            if (Assemblies != null)
            {
                _loadStatistics.AssembliesByFolderType = Assemblies
                    .GroupBy(a => a.FileTypes.ToString())
                    .ToDictionary(g => g.Key, g => g.Count());
            }

            return _loadStatistics;
        }

        /// <summary>
        /// Starts timing an assembly load operation.
        /// </summary>
        private void StartLoadTiming()
        {
            _loadStopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Stops timing and records the elapsed time.
        /// </summary>
        private void StopLoadTiming()
        {
            if (_loadStopwatch != null)
            {
                _loadStopwatch.Stop();
                _loadStatistics.TotalLoadTime = _loadStopwatch.Elapsed;
                _loadStatistics.LastLoadTimestamp = DateTime.UtcNow;
                _loadStopwatch = null;
            }
        }

        /// <summary>
        /// Records a failed assembly load attempt.
        /// </summary>
        private void RecordLoadFailure(string path)
        {
            _loadStatistics.TotalAssembliesFailed++;
            if (!string.IsNullOrEmpty(path) && !_loadStatistics.FailedAssemblyPaths.Contains(path))
            {
                _loadStatistics.FailedAssemblyPaths.Add(path);
            }
        }

        /// <summary>
        /// Records a successful NuGet package load.
        /// </summary>
        private void RecordNuGetSuccess()
        {
            _loadStatistics.NuGetPackagesLoaded++;
        }

        /// <summary>
        /// Records a failed NuGet package load.
        /// </summary>
        private void RecordNuGetFailure()
        {
            _loadStatistics.NuGetPackagesFailed++;
        }

        /// <summary>
        /// Resets all statistics.
        /// </summary>
        private void ResetStatistics()
        {
            _loadStatistics = new AssemblyLoadStatistics();
        }

        #endregion
    }
}
