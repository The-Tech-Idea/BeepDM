using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.FileManager.Classification
{
    public sealed record ClassificationHit(string PatternKey, string Category, double Confidence, int MatchedSamples, int TotalSamples);

    public sealed class FileClassificationResult
    {
        public IReadOnlyDictionary<string, IReadOnlyList<ClassificationHit>> ColumnHits { get; init; } = new Dictionary<string, IReadOnlyList<ClassificationHit>>();
        public IReadOnlyList<string> HighSensitivityColumns { get; init; } = Array.Empty<string>();
        public bool HasSpecialCategoryData { get; init; }
        public DateTimeOffset ClassifiedAt { get; init; } = DateTimeOffset.UtcNow;
    }

    public interface IDataClassificationEngine
    {
        Task<IReadOnlyList<ClassificationHit>> ClassifyColumnAsync(string columnName, IEnumerable<string> sampleValues, int sampleSize = 500, CancellationToken ct = default);
    }

    public enum MaskingStrategy
    {
        Redact,
        PartialMask,
        Tokenize,
        FormatPreservingEncrypt,
        Synthesize,
        Generalize,
        None
    }

    public sealed class ColumnMaskingPolicy
    {
        public string PatternKey { get; init; }
        public MaskingStrategy Strategy { get; init; }
        public int PartialMaskRevealChars { get; init; } = 4;
        public string TokenizationKeyId { get; init; }
        public double GeneralizeRoundTo { get; init; }
        public bool ApplyWhenConfidence { get; init; }
    }

    public interface IDataMaskingEngine
    {
        string Mask(string value, string patternKey, ColumnMaskingPolicy policy);
    }

    public interface IMaskingPolicyStore
    {
        ColumnMaskingPolicy GetPolicy(string patternKey);
        void SetPolicy(string patternKey, ColumnMaskingPolicy policy);
        IReadOnlyDictionary<string, ColumnMaskingPolicy> GetAll();
    }
}
