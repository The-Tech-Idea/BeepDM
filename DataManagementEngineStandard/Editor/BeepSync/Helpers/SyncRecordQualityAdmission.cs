using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Rules;

namespace TheTechIdea.Beep.Editor.BeepSync.Helpers
{
    internal sealed class SyncRecordQualityAdmission : IImportRecordAdmission
    {
        private readonly IRuleEngine _engine;
        private readonly string[] _keys;
        private readonly QualityFailureMode _mode;
        private readonly DataQualityAction _action;
        private readonly string _schemaId;
        private readonly int _depth;
        private readonly int _timeout;
        private readonly string _destination;
        public bool RequiresRejectStore => _action == DataQualityAction.Quarantine;
        internal int TimeoutMs => _timeout;
        internal QualityFailureMode FailureMode => _mode;

        private SyncRecordQualityAdmission(DataSyncSchema schema, IRuleEngine engine)
        {
            _engine = engine;
            if (schema.DqPolicy.RuleKeys?.Count > 128 || schema.RulePolicy?.MaxDepth < 0 || schema.RulePolicy?.MaxExecutionMs < 0)
                throw new ImportQualityAdmissionException();
            _keys = schema.DqPolicy.RuleKeys?.ToArray() ?? Array.Empty<string>();
            _mode = schema.DqPolicy.RecordFailureMode;
            _action = schema.DqPolicy.OnRecordFailure;
            _schemaId = schema.Id;
            _destination = schema.DestinationEntityName;
            _depth = schema.RulePolicy?.MaxDepth > 0 ? schema.RulePolicy.MaxDepth : 10;
            _timeout = schema.RulePolicy?.MaxExecutionMs > 0 ? schema.RulePolicy.MaxExecutionMs : 5000;
            if (_keys.Length > 128 || _keys.Any(string.IsNullOrWhiteSpace) ||
                _keys.Distinct(StringComparer.Ordinal).Count() != _keys.Length ||
                !Enum.IsDefined(typeof(QualityFailureMode), _mode) || !Enum.IsDefined(typeof(DataQualityAction), _action) ||
                _depth > 64 || _timeout > 60000)
                throw new ImportQualityAdmissionException();
        }

        private SyncRecordQualityAdmission(SyncRecordQualityAdmission source, string destination)
        {
            _engine = source._engine; _keys = source._keys; _mode = source._mode; _action = source._action;
            _schemaId = source._schemaId; _depth = source._depth; _timeout = source._timeout; _destination = destination;
        }

        internal static SyncRecordQualityAdmission Capture(DataSyncSchema schema, IRuleEngine engine, CancellationToken token)
        {
            if (schema.DqPolicy?.Enabled != true || !(schema.DqPolicy.RuleKeys?.Count > 0)) return null;
            var gate = new SyncRecordQualityAdmission(schema, engine);
            gate.Validate(token);
            return gate;
        }

        internal IImportRecordAdmission ForDirection(string destination) => new SyncRecordQualityAdmission(this, destination);

        public void Validate(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var elapsed = Stopwatch.StartNew();
            try
            {
                if (_engine == null || _keys.Any(key => !_engine.HasRule(key))) throw new ImportQualityAdmissionException();
                if (elapsed.ElapsedMilliseconds > _timeout) throw new ImportQualityAdmissionException();
            }
            catch (Exception) when (_mode == QualityFailureMode.Advisory && !token.IsCancellationRequested) { }
            catch (Exception) { token.ThrowIfCancellationRequested(); throw new ImportQualityAdmissionException(); }
            token.ThrowIfCancellationRequested();
        }

        public ImportRecordAdmissionResult Evaluate(object record, CancellationToken token)
        {
            var elapsed = Stopwatch.StartNew();
            bool warning = false;
            bool failed = false;
            foreach (var key in _keys)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    if (_engine == null || !_engine.HasRule(key)) throw new InvalidOperationException();
                    var (_, result) = _engine.SolveRule(key, new Dictionary<string, object>
                    {
                        ["record"] = record, ["entityName"] = _destination, ["schemaId"] = _schemaId
                    }, new RuleExecutionPolicy { MaxDepth = _depth, MaxExecutionMs = _timeout, AllowDeprecatedExecution = false });
                    token.ThrowIfCancellationRequested();
                    if (elapsed.ElapsedMilliseconds > _timeout || result is not bool passed) throw new InvalidOperationException();
                    if (!passed)
                    {
                        if (_action != DataQualityAction.Warn) return ImportRecordAdmissionResult.Reject(_action);
                        warning = true;
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
                catch (Exception)
                {
                    if (_mode == QualityFailureMode.Required) return ImportRecordAdmissionResult.Fail();
                    warning = failed = true;
                }
            }
            return warning ? ImportRecordAdmissionResult.Warn(failed) : ImportRecordAdmissionResult.Pass();
        }
    }
}
