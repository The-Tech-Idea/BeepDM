using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Services.Persistence
{
    /// <summary>Durable admission for cooperating local-filesystem migration executors.</summary>
    public sealed class FileMigrationExecutionOwnership : IMigrationExecutionOwnership
    {
        private readonly string _root;
        private static readonly string[] Properties = { "FormatVersion", "TargetKey", "ClaimId", "ExecutionToken",
            "PlanHash", "Revision", "State", "Disposition", "UpdatedOnUtc", "Actor", "EvidenceReference" };

        public FileMigrationExecutionOwnership(string root)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(root);
            _root = Path.GetFullPath(root);
        }

        public MigrationExecutionAdmission TryAcquireMigrationExecution(string targetIdentity, string executionToken,
            string planHash, CancellationToken token = default)
        {
            FileStream owner = null;
            try
            {
                token.ThrowIfCancellationRequested();
                var key = TargetKey(targetIdentity);
                ValidateText(executionToken, 256);
                ValidateText(planHash, 256);
                var path = ClaimPath(key);
                Directory.CreateDirectory(_root);
                owner = TryOpenOwner(path);
                if (owner == null) return new MigrationExecutionAdmission(MigrationAdmissionStatus.Busy);
                Document admitted = null;
                Document existing = null;
                AtomicFileStore.UpdateText(path, current =>
                {
                    existing = Read(current, key);
                    if (existing != null && existing.State != MigrationClaimState.Released) return current;
                    var revision = checked((existing?.Revision ?? 0) + 1);
                    if (existing != null) ArchiveReleased(existing, token);
                    admitted = new Document
                    {
                        TargetKey = key, ClaimId = Guid.NewGuid().ToString("N"), ExecutionToken = executionToken,
                        PlanHash = planHash, Revision = revision,
                        State = MigrationClaimState.Owned, UpdatedOnUtc = DateTimeOffset.UtcNow
                    };
                    return Write(admitted);
                }, token);
                if (admitted == null)
                    return new MigrationExecutionAdmission(MigrationAdmissionStatus.RequiresReconciliation,
                        existingClaim: Snapshot(existing));
                var lease = new Lease(path, admitted, owner);
                owner = null;
                return new MigrationExecutionAdmission(MigrationAdmissionStatus.Acquired, lease);
            }
            catch (OperationCanceledException) { return new MigrationExecutionAdmission(MigrationAdmissionStatus.Cancelled); }
            catch (Exception ex)
            {
                return new MigrationExecutionAdmission(ex is NotSupportedException
                    ? MigrationAdmissionStatus.Unsupported : MigrationAdmissionStatus.Failed, errorCode: ex.GetType().Name);
            }
            finally { owner?.Dispose(); }
        }

        public MigrationExecutionClaim ReadMigrationExecutionClaim(string targetIdentity)
        {
            try
            {
                var key = TargetKey(targetIdentity);
                return Snapshot(Read(AtomicFileStore.ReadText(ClaimPath(key)), key));
            }
            catch (NotSupportedException) { throw new NotSupportedException("Migration claim version is unsupported."); }
            catch (Exception ex)
            {
                throw new InvalidDataException("Migration claim could not be read (" + ex.GetType().Name + ").");
            }
        }

        public PersistenceWriteResult ReconcileMigrationExecution(string targetIdentity, string expectedClaimId,
            string actor, string evidenceReference, CancellationToken token = default) => Acknowledge(() =>
        {
            token.ThrowIfCancellationRequested();
            var key = TargetKey(targetIdentity);
            ValidateText(expectedClaimId, 32);
            ValidateText(actor, 256);
            ValidateText(evidenceReference, 1024);
            var path = ClaimPath(key);
            Directory.CreateDirectory(_root);
            using var owner = TryOpenOwner(path)
                ?? throw new InvalidOperationException("A live migration owner prevents reconciliation.");
            AtomicFileStore.UpdateText(path, current =>
            {
                var document = Read(current, key);
                if (document == null || document.ClaimId != expectedClaimId || document.State == MigrationClaimState.Released)
                    throw new InvalidOperationException("Migration claim changed or is already released.");
                document.Revision = checked(document.Revision + 1);
                document.State = MigrationClaimState.Released;
                document.Disposition = MigrationClaimDisposition.OperatorReconciled;
                document.Actor = actor;
                document.EvidenceReference = evidenceReference;
                document.UpdatedOnUtc = DateTimeOffset.UtcNow;
                return Write(document);
            }, token);
        });

        private string ClaimPath(string key) => Path.Combine(_root, "claim-v1-" + key + ".json");

        private void ArchiveReleased(Document document, CancellationToken token)
        {
            var path = Path.Combine(_root, "Completed", document.TargetKey + "-" + document.ClaimId + ".json");
            var snapshot = Write(document);
            AtomicFileStore.UpdateText(path, existing =>
            {
                if (existing != null && !string.Equals(existing, snapshot, StringComparison.Ordinal))
                    throw new InvalidDataException("Immutable migration claim evidence differs from its released snapshot.");
                return snapshot;
            }, token);
        }

        private static string TargetKey(string identity)
        {
            ValidateText(identity, 1024);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        }

        private static void ValidateText(string value, int limit)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > limit || value != value.Trim() || value.Any(char.IsControl) ||
                value.Any(char.IsSurrogate))
                throw new ArgumentException("Migration ownership identity/evidence is missing or invalid.");
        }

        private static FileStream TryOpenOwner(string path)
        {
            try
            {
                // Keep this inode/path permanently; deleting a sidecar allows ABA owners on Unix.
                return new FileStream(path + ".owner.lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException ex) when ((ex.HResult & 0xFFFF) is 32 or 33 or 11) { return null; }
        }

        private static Document Read(string content, string key)
        {
            if (content == null) return null;
            if (string.IsNullOrWhiteSpace(content) || content.Length > 16384)
                throw new InvalidDataException("Migration claim is empty or exceeds its supported envelope size.");
            using var reader = new JsonTextReader(new StringReader(content)) { MaxDepth = 8, DateParseHandling = DateParseHandling.None };
            var json = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (reader.Read() || json.Properties().Any(p => !Properties.Contains(p.Name, StringComparer.Ordinal)) ||
                json.Properties().Count() != Properties.Length)
                throw new InvalidDataException("Migration claim envelope is incomplete or contains unsupported content.");
            if (json["FormatVersion"]?.Type != JTokenType.Integer || (int)json["FormatVersion"] != 1)
                throw new NotSupportedException("Migration claim version is unsupported.");
            foreach (var name in new[] { "TargetKey", "ClaimId", "ExecutionToken", "PlanHash", "State", "UpdatedOnUtc" })
                if (json[name]?.Type != JTokenType.String) throw new InvalidDataException("Migration claim identity is invalid.");
            if (json["Revision"]?.Type != JTokenType.Integer || !Enum.TryParse((string)json["State"], out MigrationClaimState state) ||
                !Enum.IsDefined(state) || (string)json["State"] != state.ToString())
                throw new InvalidDataException("Migration claim state/revision is invalid.");
            var document = new Document
            {
                TargetKey = (string)json["TargetKey"], ClaimId = (string)json["ClaimId"],
                ExecutionToken = (string)json["ExecutionToken"], PlanHash = (string)json["PlanHash"],
                Revision = (long)json["Revision"], State = state,
                UpdatedOnUtc = DateTimeOffset.ParseExact((string)json["UpdatedOnUtc"], "O", System.Globalization.CultureInfo.InvariantCulture)
            };
            if (document.TargetKey != key || !Guid.TryParseExact(document.ClaimId, "N", out _) || document.Revision <= 0 ||
                document.UpdatedOnUtc.Offset != TimeSpan.Zero)
                throw new InvalidDataException("Migration claim identity/revision does not match its storage key.");
            ValidateText(document.ExecutionToken, 256);
            ValidateText(document.PlanHash, 256);
            document.Actor = OptionalText(json["Actor"], 256);
            document.EvidenceReference = OptionalText(json["EvidenceReference"], 1024);
            var disposition = json["Disposition"];
            if (disposition.Type != JTokenType.Null)
            {
                if (disposition.Type != JTokenType.String || !Enum.TryParse((string)disposition, out MigrationClaimDisposition parsed) ||
                    !Enum.IsDefined(parsed) || (string)disposition != parsed.ToString())
                    throw new InvalidDataException("Migration claim disposition is invalid.");
                document.Disposition = parsed;
            }
            if (state == MigrationClaimState.Owned && document.Disposition != null ||
                state == MigrationClaimState.RequiresReconciliation && document.Disposition != MigrationClaimDisposition.RequiresReconciliation ||
                state == MigrationClaimState.Released && document.Disposition is not (MigrationClaimDisposition.Completed or
                    MigrationClaimDisposition.SafeToRetry or MigrationClaimDisposition.OperatorReconciled) ||
                document.Disposition == MigrationClaimDisposition.OperatorReconciled && (document.Actor == null || document.EvidenceReference == null) ||
                document.Disposition != MigrationClaimDisposition.OperatorReconciled && (document.Actor != null || document.EvidenceReference != null))
                throw new InvalidDataException("Migration claim lifecycle/evidence is inconsistent.");
            return document;
        }

        private static string OptionalText(JToken token, int limit)
        {
            if (token.Type == JTokenType.Null) return null;
            if (token.Type != JTokenType.String) throw new InvalidDataException("Migration claim evidence is invalid.");
            var text = (string)token;
            ValidateText(text, limit);
            return text;
        }

        private static string Write(Document d) => new JObject
        {
            ["FormatVersion"] = 1, ["TargetKey"] = d.TargetKey, ["ClaimId"] = d.ClaimId,
            ["ExecutionToken"] = d.ExecutionToken, ["PlanHash"] = d.PlanHash, ["Revision"] = d.Revision,
            ["State"] = d.State.ToString(), ["Disposition"] = d.Disposition?.ToString(),
            ["UpdatedOnUtc"] = d.UpdatedOnUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            ["Actor"] = d.Actor, ["EvidenceReference"] = d.EvidenceReference
        }.ToString(Formatting.None);

        private static MigrationExecutionClaim Snapshot(Document d) => d == null ? null : new MigrationExecutionClaim(
            d.TargetKey, d.ClaimId, d.ExecutionToken, d.PlanHash, d.Revision, d.State, d.Disposition, d.UpdatedOnUtc);

        private static PersistenceWriteResult Acknowledge(Action action)
        {
            try { action(); return new PersistenceWriteResult(PersistenceWriteStatus.Saved); }
            catch (OperationCanceledException) { return new PersistenceWriteResult(PersistenceWriteStatus.Cancelled); }
            catch (Exception ex)
            {
                // Do not expose paths, target identity, actor/evidence or nested storage exception messages.
                return new PersistenceWriteResult(ex is NotSupportedException ? PersistenceWriteStatus.Unsupported : PersistenceWriteStatus.Failed,
                    new InvalidOperationException("Migration ownership persistence was not acknowledged (" + ex.GetType().Name + ")."));
            }
        }

        private sealed class Document
        {
            public string TargetKey, ClaimId, ExecutionToken, PlanHash, Actor, EvidenceReference;
            public long Revision;
            public MigrationClaimState State;
            public MigrationClaimDisposition? Disposition;
            public DateTimeOffset UpdatedOnUtc;
        }

        private sealed class Lease : IMigrationExecutionLease
        {
            private readonly object _gate = new object();
            private readonly string _path;
            private Document _document;
            private FileStream _owner;
            private bool _finished;

            public Lease(string path, Document document, FileStream owner)
            { _path = path; _document = document; _owner = owner; }

            public MigrationExecutionClaim Claim { get { lock (_gate) return Snapshot(_document); } }

            public PersistenceWriteResult Finish(MigrationClaimDisposition disposition, CancellationToken token = default)
            {
                lock (_gate)
                {
                    return Acknowledge(() =>
                    {
                        if (_owner == null || _finished) throw new InvalidOperationException("Migration owner is disposed or already finished.");
                        if (disposition is not (MigrationClaimDisposition.Completed or MigrationClaimDisposition.SafeToRetry or MigrationClaimDisposition.RequiresReconciliation))
                            throw new ArgumentException("Migration owner cannot assert operator reconciliation.");
                        Document saved = null;
                        AtomicFileStore.UpdateText(_path, current =>
                        {
                            var document = Read(current, _document.TargetKey);
                            if (document == null || document.ClaimId != _document.ClaimId || document.Revision != _document.Revision ||
                                document.State != MigrationClaimState.Owned)
                                throw new InvalidOperationException("Migration claim changed before completion.");
                            document.Revision = checked(document.Revision + 1);
                            document.State = disposition == MigrationClaimDisposition.RequiresReconciliation
                                ? MigrationClaimState.RequiresReconciliation : MigrationClaimState.Released;
                            document.Disposition = disposition;
                            document.UpdatedOnUtc = DateTimeOffset.UtcNow;
                            saved = document;
                            return Write(document);
                        }, token);
                        _document = saved;
                        _finished = true;
                    });
                }
            }

            public void Dispose()
            {
                lock (_gate) { _owner?.Dispose(); _owner = null; }
            }
        }
    }
}
