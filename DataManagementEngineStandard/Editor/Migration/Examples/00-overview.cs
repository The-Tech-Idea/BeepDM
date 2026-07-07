// Example 00 — MigrationManager Examples Overview
//
// This folder contains end-to-end examples for the current MigrationManager API.
// Each example is a static class with a `Run(...)` entry point that the host can invoke.
//
// Execution order:
//   1. 01-plan-and-policy.cs
//   2. 02-dryrun-preflight-impact.cs
//   3. 03-execution-checkpoint-resume.cs
//   4. 04-rollback-compensation.cs
//   5. 05-ci-and-artifacts.cs (artifacts are produced by BuildImpactReport + ExportSnapshot)
//   6. 06-rollout-governance.cs
//   7. 07-efcore-interop.cs — bridge an EF Core / ORM MigrationModel
//
// All examples assume:
//   - IDMEEditor is initialised
//   - IDataSource is configured and reachable
//   - MigrationManager is constructed as: new MigrationManager(editor, dataSource)

using System;

namespace TheTechIdea.Beep.Editor.Migration.Examples
{
    /// <summary>
    /// Marker class that documents the example order. Run any individual example
    /// from a test harness by calling its static `Run(...)` method.
    /// </summary>
    public static class Example00_Overview
    {
        public static void Run()
        {
            Console.WriteLine("MigrationManager examples — see individual files for Run(...) entry points.");
        }
    }
}
