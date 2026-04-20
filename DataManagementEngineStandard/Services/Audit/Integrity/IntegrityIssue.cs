using System;

namespace TheTechIdea.Beep.Services.Audit.Integrity
{
    /// <summary>
    /// Diagnostic payload describing a single chain divergence. The
    /// verifier returns at most one of these per call (the first
    /// failure) so reports are actionable rather than overwhelming.
    /// </summary>
    public sealed class IntegrityIssue
    {
        /// <summary>Kind of divergence detected.</summary>
        public IntegrityIssueKind Kind { get; }

        /// <summary>Chain identifier in which the divergence was found.</summary>
        public string ChainId { get; }

        /// <summary>Sequence number of the offending event.</summary>
        public long Sequence { get; }

        /// <summary>Event identifier of the offending event, if known.</summary>
        public Guid? EventId { get; }

        /// <summary>Expected value (hash or sequence), as a hex/integer string.</summary>
        public string Expected { get; }

        /// <summary>Actual value observed, as a hex/integer string.</summary>
        public string Actual { get; }

        /// <summary>Human-readable explanation suitable for the runbook.</summary>
        public string Message { get; }

        /// <summary>Creates an integrity issue payload.</summary>
        public IntegrityIssue(
            IntegrityIssueKind kind,
            string chainId,
            long sequence,
            Guid? eventId,
            string expected,
            string actual,
            string message)
        {
            Kind = kind;
            ChainId = chainId;
            Sequence = sequence;
            EventId = eventId;
            Expected = expected;
            Actual = actual;
            Message = message;
        }
    }
}
