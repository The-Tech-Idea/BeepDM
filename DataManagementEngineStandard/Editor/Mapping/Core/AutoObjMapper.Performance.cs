using System;
using TheTechIdea.Beep.Editor.Mapping.Helpers;

namespace TheTechIdea.Beep.Editor.Mapping
{
    /// <summary>
    /// AutoObjMapper - Performance and Monitoring functionality
    /// </summary>
    public sealed partial class AutoObjMapper
    {
        private MappingPerformanceHelper _performanceHelper;

        /// <summary>
        /// Gets or creates the performance helper
        /// </summary>
        private MappingPerformanceHelper PerformanceHelper => 
            _performanceHelper ??= new MappingPerformanceHelper(_options);

        /// <summary>
        /// Maps source object to destination with performance monitoring
        /// </summary>
        public TDest MapWithMonitoring<TSource, TDest>(TSource source, TDest destination)
        {
            if (!_options.EnableStatistics)
                return Map(source, destination);

            return PerformanceMonitor.ExecuteWithMonitoring(
                () => Map(source, destination),
                (duration, success) => PerformanceHelper.RecordMapping<TSource, TDest>(duration, success)
            );
        }

        /// <summary>
        /// Maps source object to new destination instance with performance monitoring
        /// </summary>
        public TDest MapWithMonitoring<TSource, TDest>(TSource source) where TDest : new()
        {
            if (!_options.EnableStatistics)
                return Map<TSource, TDest>(source);

            return PerformanceMonitor.ExecuteWithMonitoring(
                () => Map<TSource, TDest>(source),
                (duration, success) => PerformanceHelper.RecordMapping<TSource, TDest>(duration, success)
            );
        }

        /// <summary>
        /// Gets performance metrics for a specific type mapping
        /// </summary>
        public MappingPerformanceMetrics GetMappingMetrics<TSource, TDest>()
        {
            return PerformanceHelper?.GetMetrics<TSource, TDest>() ?? new MappingPerformanceMetrics();
        }

        /// <summary>
        /// Clears performance metrics
        /// </summary>
        public void ClearPerformanceMetrics()
        {
            PerformanceHelper?.ClearMetrics();
        }
    }
}