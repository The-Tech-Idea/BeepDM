using System;
using System.Threading;

namespace TheTechIdea.Beep.Editor.Importing
{
    public enum ImportTransformationStage { Input, Projection, Mapping, Defaults, Custom, Completed }

    /// <summary>Admission evidence only; it does not undo mutations made by a callback.</summary>
    public sealed class ImportTransformationResult
    {
        private ImportTransformationResult(object record, ImportTransformationStage stage, string exceptionType)
        {
            Record = record;
            Stage = stage;
            ExceptionType = exceptionType;
        }

        public object Record { get; }
        public ImportTransformationStage Stage { get; }
        public string ExceptionType { get; }
        public bool Succeeded => Stage == ImportTransformationStage.Completed && Record != null;
        public string Message => Succeeded ? "Transformation completed." : $"Required transformation failed at {Stage}.";

        public static ImportTransformationResult Success(object record) => record != null
            ? new ImportTransformationResult(record, ImportTransformationStage.Completed, null)
            : Failure(ImportTransformationStage.Input);

        public static ImportTransformationResult Failure(ImportTransformationStage stage, string exceptionType = null) =>
            new ImportTransformationResult(null,
                stage == ImportTransformationStage.Completed ? ImportTransformationStage.Input : stage, exceptionType);
    }

    /// <summary>Optional typed transformation outcome without changing legacy helper signatures.</summary>
    public interface IDataImportTransformationOutcome
    {
        ImportTransformationResult TransformRecord(object record, DataImportConfiguration config, CancellationToken token);
    }

    /// <summary>Safe legacy adapter failure, without a raw row value or inner exception.</summary>
    public sealed class ImportTransformationException : InvalidOperationException
    {
        public ImportTransformationException(ImportTransformationStage stage, string exceptionType = null)
            : base($"Required transformation failed at {stage}.")
        {
            Stage = stage;
            ExceptionType = exceptionType;
        }

        public ImportTransformationStage Stage { get; }
        public string ExceptionType { get; }
    }
}
