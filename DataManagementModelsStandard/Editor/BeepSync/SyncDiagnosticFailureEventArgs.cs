using System;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>Non-fatal diagnostic failure; contains no raw exception, credentials or row data.</summary>
    public sealed class SyncDiagnosticFailureEventArgs : EventArgs
    {
        public string Operation { get; }
        public string ExceptionType { get; }

        public SyncDiagnosticFailureEventArgs(string operation, string exceptionType)
        {
            Operation = operation;
            ExceptionType = exceptionType;
        }
    }
}
