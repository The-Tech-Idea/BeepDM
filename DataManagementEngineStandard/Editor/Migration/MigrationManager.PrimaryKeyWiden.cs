using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RdbmsHelpers;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    /// <summary>
    /// Widening a primary-key column. This is deliberately NOT part of <see cref="ISchemaMigrationProvider"/>
    /// — that interface has 23+ non-RDBMS implementers (vector/messaging/NoSQL datasources) that would
    /// gain an unusable member for a capability only SQL-speaking RDBMS providers can ever perform.
    /// Implemented directly against <see cref="RdbmsHelper"/> instead, resolved the same way
    /// <c>RdbmsSqlMigrationProvider.Helper</c> resolves it.
    ///
    /// Always drops and recreates the constraint rather than trying a plain <c>AlterColumn</c> first
    /// — live-verified both ways: SQL Server accepts an in-place widen of a PRIMARY KEY column when
    /// the underlying type kind is unchanged (e.g. NVARCHAR(1) → NVARCHAR(4000)), but REJECTS it when
    /// the type kind also changes (e.g. CHAR(1) → NVARCHAR(4000), exactly what a table created under
    /// the pre-fix BeepDM type-mapping defect has). Drop/recreate handles both cases uniformly; a
    /// plain alter does not, and this pass cannot know in advance which case a given live column is.
    /// </summary>
    public partial class MigrationManager
    {
        /// <summary>
        /// Widens an existing PRIMARY KEY column. SQL Server (and most RDBMS engines) refuse a plain
        /// <c>ALTER COLUMN</c> on a column a PRIMARY KEY constraint depends on when the change
        /// includes a type-kind change (not just a size increase), so this drops the constraint,
        /// alters the column, then always re-adds the constraint — even when the alter step failed —
        /// so the table is never left without its primary key. The constraint name is resolved live
        /// (SQL Server auto-names it; there is no fixed naming convention to assume).
        /// </summary>
        public IErrorsInfo WidenPrimaryKeyColumn(string entityName, EntityField newColumn)
        {
            if (string.IsNullOrWhiteSpace(entityName) || newColumn == null || string.IsNullOrWhiteSpace(newColumn.FieldName))
                return CreateErrorsInfo(Errors.Failed, "Entity or column information is missing");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            if (_editor?.GetDataSourceHelper(MigrateDataSource.DatasourceType) is not RdbmsHelper helper)
                return CreateErrorsInfo(Errors.Failed,
                    $"Primary-key widen is only supported for RDBMS SQL providers (datasource type: {MigrateDataSource.DatasourceType}).");

            // 1. Resolve the live constraint name — never assumed.
            var (pkQuery, pkQueryOk, pkQueryErr) = helper.GetPrimaryKeyQuery(entityName);
            if (!pkQueryOk)
                return CreateErrorsInfo(Errors.Failed, $"Could not build primary-key lookup query for '{entityName}': {pkQueryErr}");

            string constraintName;
            try
            {
                constraintName = MigrateDataSource.RunQuery(pkQuery)
                    ?.Cast<object[]>()
                    .FirstOrDefault()
                    ?.FirstOrDefault()
                    ?.ToString();
            }
            catch (Exception ex)
            {
                return CreateErrorsInfo(Errors.Failed, $"Could not resolve primary-key constraint name for '{entityName}': {ex.Message}", ex);
            }

            if (string.IsNullOrWhiteSpace(constraintName))
                return CreateErrorsInfo(Errors.Failed, $"'{entityName}' has no discoverable primary-key constraint — nothing to widen.");

            // 2. Drop the constraint.
            var (dropSql, dropOk, dropErr) = helper.GenerateDropPrimaryKeySql(entityName, constraintName);
            if (!dropOk)
                return CreateErrorsInfo(Errors.Failed, $"Could not generate DROP CONSTRAINT for '{entityName}.{constraintName}': {dropErr}");

            var dropResult = MigrateDataSource.ExecuteSql(dropSql);
            TrackMigration("DropPrimaryKey", entityName, newColumn.FieldName, dropSql, dropResult);
            EmitDdlEvidence("DropPrimaryKey", entityName, newColumn.FieldName, null, dropSql,
                OutcomeFor(dropResult), DdlHelperSource.Direct,
                dropResult?.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");

            if (dropResult?.Flag != Errors.Ok)
                return CreateErrorsInfo(Errors.Failed, $"Could not drop primary key '{constraintName}' on '{entityName}': {dropResult?.Message}", dropResult?.Ex);

            // 3. Alter the column. Whatever happens here, step 4 always runs — the table must not be
            //    left without its primary key even if the widen itself fails.
            var (alterSql, alterOk, alterErr) = helper.GenerateAlterColumnSql(entityName, newColumn.FieldName, newColumn);
            IErrorsInfo alterResult = alterOk
                ? MigrateDataSource.ExecuteSql(alterSql)
                : CreateErrorsInfo(Errors.Failed, alterErr);
            TrackMigration("AlterColumn", entityName, newColumn.FieldName, alterSql ?? string.Empty, alterResult);
            EmitDdlEvidence("AlterColumn", entityName, newColumn.FieldName, null, alterSql,
                OutcomeFor(alterResult), DdlHelperSource.Direct,
                alterResult?.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");

            // 4. Always re-add the primary key.
            var (addSql, addOk, addErr) = helper.GenerateAddPrimaryKeySql(entityName, newColumn.FieldName);
            IErrorsInfo addResult = addOk
                ? MigrateDataSource.ExecuteSql(addSql)
                : CreateErrorsInfo(Errors.Failed, addErr);
            TrackMigration("AddPrimaryKey", entityName, newColumn.FieldName, addSql ?? string.Empty, addResult);
            EmitDdlEvidence("AddPrimaryKey", entityName, newColumn.FieldName, null, addSql,
                OutcomeFor(addResult), DdlHelperSource.Direct,
                addResult?.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");

            if (addResult?.Flag != Errors.Ok)
                return CreateErrorsInfo(Errors.Failed,
                    $"Primary key on '{entityName}' could not be recreated after widening '{newColumn.FieldName}' — " +
                    $"the table has NO primary key until this is corrected manually: {addResult?.Message}", addResult?.Ex);

            if (alterResult?.Flag != Errors.Ok)
                return CreateErrorsInfo(Errors.Failed,
                    $"Could not widen '{entityName}.{newColumn.FieldName}' (primary key restored, unchanged): {alterResult?.Message}", alterResult?.Ex);

            return CreateErrorsInfo(Errors.Ok, $"Widened primary key column '{entityName}.{newColumn.FieldName}'.");
        }
    }
}
