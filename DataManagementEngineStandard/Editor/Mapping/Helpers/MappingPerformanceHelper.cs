using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TheTechIdea.Beep.Editor.Mapping.Helpers
{
    /// <summary>
    /// Helper class for mapping performance monitoring and validation
    /// </summary>
    public class MappingPerformanceHelper
    {
        private readonly AutoObjMapperOptions _options;
        private readonly Dictionary<(Type src, Type dest), MappingPerformanceMetrics> _metrics;

        public MappingPerformanceHelper(AutoObjMapperOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _metrics = new Dictionary<(Type src, Type dest), MappingPerformanceMetrics>();
        }

        /// <summary>
        /// Records mapping performance metrics
        /// </summary>
        public void RecordMapping<TSource, TDest>(TimeSpan duration, bool success)
        {
            if (!_options.EnableStatistics) return;

            var key = (typeof(TSource), typeof(TDest));
            if (!_metrics.TryGetValue(key, out var metrics))
            {
                metrics = new MappingPerformanceMetrics();
                _metrics[key] = metrics;
            }

            metrics.TotalMappings++;
            metrics.TotalDuration += duration;
            if (success)
                metrics.SuccessfulMappings++;
            else
                metrics.FailedMappings++;

            if (duration > metrics.MaxDuration)
                metrics.MaxDuration = duration;

            if (duration < metrics.MinDuration || metrics.MinDuration == TimeSpan.Zero)
                metrics.MinDuration = duration;
        }

        /// <summary>
        /// Gets performance metrics for a specific type pair
        /// </summary>
        public MappingPerformanceMetrics GetMetrics<TSource, TDest>()
        {
            var key = (typeof(TSource), typeof(TDest));
            return _metrics.TryGetValue(key, out var metrics) ? metrics : new MappingPerformanceMetrics();
        }

        /// <summary>
        /// Gets all performance metrics
        /// </summary>
        public Dictionary<(Type src, Type dest), MappingPerformanceMetrics> GetAllMetrics()
        {
            return new Dictionary<(Type src, Type dest), MappingPerformanceMetrics>(_metrics);
        }

        /// <summary>
        /// Clears all performance metrics
        /// </summary>
        public void ClearMetrics()
        {
            _metrics.Clear();
        }
    }

    /// <summary>
    /// Performance metrics for a specific type pair mapping
    /// </summary>
    public class MappingPerformanceMetrics
    {
        public long TotalMappings { get; set; }
        public long SuccessfulMappings { get; set; }
        public long FailedMappings { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public TimeSpan MinDuration { get; set; }

        public TimeSpan AverageDuration => TotalMappings > 0 ? 
            TimeSpan.FromTicks(TotalDuration.Ticks / TotalMappings) : TimeSpan.Zero;

        public double SuccessRate => TotalMappings > 0 ? 
            (double)SuccessfulMappings / TotalMappings * 100 : 0;
    }

    /// <summary>
    /// Helper for executing actions with performance monitoring
    /// </summary>
    public static class PerformanceMonitor
    {
        /// <summary>
        /// Executes an action and measures its performance
        /// </summary>
        public static T ExecuteWithMonitoring<T>(Func<T> action, Action<TimeSpan, bool> onComplete = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var success = false;
            T result = default;

            try
            {
                result = action();
                success = true;
                return result;
            }
            catch
            {
                success = false;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                onComplete?.Invoke(stopwatch.Elapsed, success);
            }
        }

        /// <summary>
        /// Executes an action and measures its performance
        /// </summary>
        public static void ExecuteWithMonitoring(Action action, Action<TimeSpan, bool> onComplete = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var success = false;

            try
            {
                action();
                success = true;
            }
            catch
            {
                success = false;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                onComplete?.Invoke(stopwatch.Elapsed, success);
            }
        }
    }
}