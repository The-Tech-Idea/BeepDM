using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Migration;

namespace TheTechIdea.Beep.SetUp
{
    public class SetupContext
    {
        public IDMEEditor Editor { get; set; }
        public IDataSource DataSource { get; set; }
        public SetupOptions Options { get; set; } = new SetupOptions();
        public SetupState State { get; set; } = new SetupState();
        public ISetupProgressReporter ProgressReporter { get; set; }

        public ConnectionProperties ConnectionProperties { get; set; }
        public MigrationPlanArtifact MigrationPlan { get; set; }
        public MigrationExecutionResult MigrationResult { get; set; }

        public IReadOnlyCollection<string> CompletedSeederIds =>
            State?.CompletedSeederIds ?? (IReadOnlyCollection<string>)Array.Empty<string>();

        public ConcurrentDictionary<string, object> Properties { get; } = new();

        public ConnectionProperties? TryGetConnectionProperties()
            => ConnectionProperties;

        public T? TryGetProperty<T>(string key) where T : class
            => Properties.TryGetValue(key, out var v) ? v as T : null;

        // Skill: schema-drift is engine-specific. Live on the Properties dictionary keyed
        // by "SchemaDrift" (or via TryGetProperty<T>). The engine can add typed helpers
        // (e.g. an extension method in BeepDM) that cast the dictionary back to its shape.
        public void SetDryRunReport(string json)
            => Properties["DryRunReportJson"] = json;

        public string? TryGetDryRunReport()
            => TryGetProperty<string>("DryRunReportJson");

        public void SetCompensationPlan(string json)
            => Properties["CompensationPlanJson"] = json;
    }
}
