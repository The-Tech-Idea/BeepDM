using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Audit.Integrity
{
    /// <summary>
    /// Verification half of <see cref="HashChainSigner"/>. Replays the
    /// hash chain across an event sequence and reports the first
    /// divergence found. Used by the <see cref="IntegrityVerifier"/>
    /// runbook entry-points and any periodic background check.
    /// </summary>
    public sealed partial class HashChainSigner
    {
        /// <inheritdoc/>
        public IntegrityCheckResult VerifyChain(IEnumerable<AuditEvent> events, ChainAnchor expectedAnchor = null)
        {
            if (events is null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            byte[] key = _keyProvider.GetHmacKey() ?? Array.Empty<byte>();
            long inspected = 0;
            string priorChainId = null;
            long priorSequence = 0;
            string priorHash = string.Empty;
            bool first = true;

            foreach (AuditEvent ev in events)
            {
                if (ev is null)
                {
                    continue;
                }
                inspected++;

                string chainId = string.IsNullOrEmpty(ev.ChainId) ? AuditEvent.DefaultChainId : ev.ChainId;

                if (first)
                {
                    priorChainId = chainId;
                    first = false;
                }
                else if (!string.Equals(priorChainId, chainId, StringComparison.Ordinal))
                {
                    return IntegrityCheckResult.Fail(inspected, new IntegrityIssue(
                        IntegrityIssueKind.ChainIdMismatch,
                        priorChainId,
                        ev.Sequence,
                        ev.EventId,
                        priorChainId,
                        chainId,
                        $"Event {ev.EventId} carries chainId '{chainId}' but the chain under verification is '{priorChainId}'."));
                }

                long expectedSeq = priorSequence + 1;
                if (ev.Sequence != expectedSeq)
                {
                    return IntegrityCheckResult.Fail(inspected, new IntegrityIssue(
                        IntegrityIssueKind.SequenceGap,
                        chainId,
                        ev.Sequence,
                        ev.EventId,
                        expectedSeq.ToString(),
                        ev.Sequence.ToString(),
                        $"Sequence gap in chain '{chainId}': expected {expectedSeq}, found {ev.Sequence}."));
                }

                string prev = ev.PrevHash ?? string.Empty;
                if (!string.Equals(prev, priorHash, StringComparison.Ordinal))
                {
                    return IntegrityCheckResult.Fail(inspected, new IntegrityIssue(
                        IntegrityIssueKind.PrevHashMismatch,
                        chainId,
                        ev.Sequence,
                        ev.EventId,
                        priorHash,
                        prev,
                        $"PrevHash mismatch at sequence {ev.Sequence} in chain '{chainId}'."));
                }

                string recomputed;
                try
                {
                    recomputed = ComputeHash(key, ev, prev);
                }
                catch (Exception ex)
                {
                    return IntegrityCheckResult.Fail(inspected, new IntegrityIssue(
                        IntegrityIssueKind.PayloadInvalid,
                        chainId,
                        ev.Sequence,
                        ev.EventId,
                        null,
                        null,
                        $"Could not hash event {ev.EventId} at sequence {ev.Sequence}: {ex.Message}"));
                }

                if (!string.Equals(recomputed, ev.Hash, StringComparison.Ordinal))
                {
                    return IntegrityCheckResult.Fail(inspected, new IntegrityIssue(
                        IntegrityIssueKind.HashMismatch,
                        chainId,
                        ev.Sequence,
                        ev.EventId,
                        recomputed,
                        ev.Hash,
                        $"Hash mismatch at sequence {ev.Sequence} in chain '{chainId}': content was modified after signing."));
                }

                priorSequence = ev.Sequence;
                priorHash = ev.Hash ?? string.Empty;
            }

            if (expectedAnchor is not null && priorChainId is not null)
            {
                if (!string.Equals(expectedAnchor.ChainId, priorChainId, StringComparison.Ordinal))
                {
                    return IntegrityCheckResult.Fail(inspected, new IntegrityIssue(
                        IntegrityIssueKind.AnchorMismatch,
                        priorChainId,
                        priorSequence,
                        null,
                        expectedAnchor.ChainId,
                        priorChainId,
                        $"Anchor chainId '{expectedAnchor.ChainId}' disagrees with verified chain '{priorChainId}'."));
                }
                if (expectedAnchor.LastSequence != priorSequence ||
                    !string.Equals(expectedAnchor.LastHash ?? string.Empty, priorHash, StringComparison.Ordinal))
                {
                    return IntegrityCheckResult.Fail(inspected, new IntegrityIssue(
                        IntegrityIssueKind.AnchorMismatch,
                        priorChainId,
                        priorSequence,
                        null,
                        $"seq={expectedAnchor.LastSequence}, hash={expectedAnchor.LastHash}",
                        $"seq={priorSequence}, hash={priorHash}",
                        $"Anchor disagrees with chain '{priorChainId}' tail."));
                }
            }

            return IntegrityCheckResult.Ok(inspected);
        }
    }
}
