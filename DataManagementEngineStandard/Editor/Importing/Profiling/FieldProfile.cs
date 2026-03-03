using System;

namespace TheTechIdea.Beep.Editor.Importing.Profiling
{
    /// <summary>Statistical profile of a single field captured during a data profiling run.</summary>
    public sealed class FieldProfile
    {
        /// <summary>Field / column name.</summary>
        public string FieldName         { get; set; } = string.Empty;

        /// <summary>Inferred CLR type name of the most common value in the sample.</summary>
        public string InferredType      { get; set; } = string.Empty;

        /// <summary>Total rows sampled (denominator for ratios).</summary>
        public int SampleCount          { get; set; }

        /// <summary>Number of rows where the field was null or empty.</summary>
        public int NullCount            { get; set; }

        /// <summary>Ratio of null values in the sample (0.0 – 1.0).</summary>
        public double NullRatio         => SampleCount == 0 ? 0 : (double)NullCount / SampleCount;

        /// <summary>Number of distinct values observed in the sample.</summary>
        public int DistinctCount        { get; set; }

        /// <summary>Minimum value (as string) observed in the sample.</summary>
        public string? MinValue         { get; set; }

        /// <summary>Maximum value (as string) observed in the sample.</summary>
        public string? MaxValue         { get; set; }

        /// <summary>Minimum string length observed (string fields only).</summary>
        public int? MinLength           { get; set; }

        /// <summary>Maximum string length observed (string fields only).</summary>
        public int? MaxLength           { get; set; }

        /// <summary>Arithmetic mean (numeric fields only).</summary>
        public double? Mean             { get; set; }

        /// <summary>Standard deviation (numeric fields only).</summary>
        public double? StdDev           { get; set; }
    }

    /// <summary>
    /// Full profile of an entity produced by <see cref="DataProfiler.ProfileAsync"/>.
    /// </summary>
    public sealed class DataProfile
    {
        /// <summary>Name of the profiled data source connection.</summary>
        public string DataSourceName    { get; set; } = string.Empty;

        /// <summary>Name of the profiled entity / table.</summary>
        public string EntityName        { get; set; } = string.Empty;

        /// <summary>UTC timestamp when the profile was captured.</summary>
        public DateTime CapturedAt      { get; set; } = DateTime.UtcNow;

        /// <summary>Number of rows used to build the profile.</summary>
        public int SampleSize           { get; set; }

        /// <summary>Per-field statistical summaries.</summary>
        public System.Collections.Generic.List<FieldProfile> Fields { get; set; } = new();
    }
}
