// Example 07 — ORM / EF Core Interop
//
// This example shows how to bridge an ORM-shaped model (the canonical example
// is Entity Framework Core) into the existing migration plan / readiness / apply
// pipeline.
//
// BeepDM does not take a hard dependency on any ORM package. Instead, callers
// populate a `MigrationModel` POCO at the call site and pass it to the engine.
// A future companion NuGet package (`TheTechIdea.Beep.DataManagementEngine.EFCore`)
// can ship a reusable `DbContext` → `MigrationModel` adapter without forcing
// every BeepDM consumer to take a hard EF Core dependency.

using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Migration;

namespace TheTechIdea.Beep.Editor.Migration.Examples
{
    /// <summary>
    /// Phase 7 — bridge an ORM/EF Core DbContext into the engine via the
    /// `MigrationModel` POCO and `BuildMigrationPlanForModel`.
    /// </summary>
    public static class Example07_EfCoreInterop
    {
        public static MigrationPlanArtifact RunFromMigrationModel(
            IDMEEditor editor,
            IDataSource dataSource,
            MigrationModel model)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (dataSource == null) throw new ArgumentNullException(nameof(dataSource));
            if (model == null) throw new ArgumentNullException(nameof(model));

            var migrationManager = new MigrationManager(editor, dataSource);
            return migrationManager.BuildMigrationPlanForModel(
                model,
                detectRelationships: true);
        }

        /// <summary>
        /// Pure reflection path that mirrors EF Core data annotations — no EF Core
        /// dependency required. Use this when the entity types are decorated with
        /// `[Table]`, `[Column]`, `[Key]`, `[Required]`, `[MaxLength]`, etc.
        /// </summary>
        public static MigrationPlanArtifact RunFromAnnotatedTypes(
            IDMEEditor editor,
            IDataSource dataSource,
            IEnumerable<Type> annotatedTypes)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (dataSource == null) throw new ArgumentNullException(nameof(dataSource));
            if (annotatedTypes == null) throw new ArgumentNullException(nameof(annotatedTypes));

            var migrationManager = new MigrationManager(editor, dataSource);
            return migrationManager.BuildMigrationPlanForTypesAnnotated(
                annotatedTypes,
                detectRelationships: true);
        }
    }
}
