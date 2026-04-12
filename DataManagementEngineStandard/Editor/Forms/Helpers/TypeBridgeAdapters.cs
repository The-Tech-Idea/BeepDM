using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;
namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>
    /// Bridges WinForms-local validation rules to the BeepDM IValidationManager API.
    /// Call from BeepDataBlock when IsCoordinated to keep FormsManager.Validation in sync.
    /// </summary>
    public static class ValidationBridge
    {
        /// <summary>
        /// Convert a local validation rule registration into a BeepDM ValidationRule
        /// and register it with ValidateManager.
        /// </summary>
        /// <param name="blockName">Block name in FormsManager</param>
        /// <param name="fieldName">Field the rule applies to ("*" = record-level)</param>
        /// <param name="ruleName">Unique rule name (nullable — auto-generated if empty)</param>
        /// <param name="validationType">Local ValidationType ordinal</param>
        /// <param name="errorMessage">Error message</param>
        /// <param name="isRequired">Required flag</param>
        /// <param name="minValue">Min value</param>
        /// <param name="maxValue">Max value</param>
        /// <param name="pattern">Regex pattern</param>
        /// <param name="executionOrder">Execution order</param>
        /// <param name="customValidator">Optional custom validator matching BeepDM signature</param>
        public static ValidationRule ToBeepDMRule(
            string blockName,
            string fieldName,
            string ruleName,
            int validationType,
            string errorMessage,
            bool isRequired,
            object minValue,
            object maxValue,
            string pattern,
            int executionOrder,
            Func<object, object, Dictionary<string, object>, Task<(bool isValid, string errorMessage)>> customValidator = null)
        {
            var dmType = MapValidationType(validationType, isRequired);

            return new ValidationRule
            {
                RuleName = ruleName ?? $"{blockName}.{fieldName}.{Guid.NewGuid():N}".Substring(0, 40),
                BlockName = blockName,
                ItemName = fieldName == "*" ? null : fieldName,
                ValidationType = dmType,
                ErrorMessage = errorMessage,
                MinValue = minValue,
                MaxValue = maxValue,
                Pattern = pattern,
                ExecutionOrder = executionOrder,
                IsEnabled = true,
                CustomValidator = customValidator,
                Severity = ValidationSeverity.Error,
                Timing = ValidationTiming.Manual
            };
        }

        private static ValidationType MapValidationType(int localType, bool isRequired)
        {
            if (isRequired) return ValidationType.Required;

            // Local enum: Required=0, Format=1, Range=2, Length=3, CrossField=4, BusinessRule=5, Lookup=6, Expression=7, Computed=8
            return localType switch
            {
                0 => ValidationType.Required,
                1 => ValidationType.Pattern,
                2 => ValidationType.Range,
                3 => ValidationType.MaxLength,
                4 => ValidationType.CrossField,
                5 => ValidationType.Custom,
                6 => ValidationType.Lookup,
                7 => ValidationType.Custom,
                8 => ValidationType.Custom,
                _ => ValidationType.Custom
            };
        }
    }

    /// <summary>
    /// Bridges WinForms-local trigger definitions to the BeepDM ITriggerManager API.
    /// </summary>
    public static class TriggerBridge
    {
        /// <summary>
        /// Map a local TriggerType int value to the BeepDM TriggerType enum.
        /// Local ranges: Form=1-6, Block=100-109, Record=200-214, Item=300-311, Nav=400-403, Error=500-502, Extra=600-604
        /// BeepDM ranges: Form=0-8, Block=20-39, Record=50-54, Item=70-99, Nav=100-119, etc.
        /// </summary>
        public static TriggerType MapTriggerType(int localType)
        {
            return localType switch
            {
                // Form-level
                1 => TriggerType.WhenNewFormInstance,
                2 => TriggerType.PreForm,
                3 => TriggerType.PostForm,
                5 => TriggerType.PreCommit,
                6 => TriggerType.PostCommit,

                // Block-level
                100 => TriggerType.WhenNewBlockInstance,
                101 => TriggerType.PreBlock,
                102 => TriggerType.PostBlock,
                103 => TriggerType.WhenRemoveRecord,   // closest: WhenClearBlock → WhenRemoveRecord
                104 => TriggerType.WhenCreateRecord,
                109 => TriggerType.OnPopulateDetails,

                // Record-level
                200 => TriggerType.WhenNewRecordInstance,
                201 => TriggerType.PreInsert,
                202 => TriggerType.PostInsert,
                203 => TriggerType.PreUpdate,
                204 => TriggerType.PostUpdate,
                205 => TriggerType.PreDelete,
                206 => TriggerType.PostDelete,
                207 => TriggerType.PreQuery,
                208 => TriggerType.PostQuery,
                209 => TriggerType.WhenNewRecordInstance, // WhenValidateRecord → closest
                210 => TriggerType.OnLock,
                211 => TriggerType.OnCheckDeleteMaster,
                212 => TriggerType.OnClearDetails,

                // Item-level
                300 => TriggerType.WhenNewItemInstance,
                301 => TriggerType.WhenValidateItem,
                302 => TriggerType.PreTextItem,
                303 => TriggerType.PostTextItem,
                304 => TriggerType.WhenListChanged,
                305 => TriggerType.KeyNextItem,
                306 => TriggerType.KeyPreviousItem,
                307 => TriggerType.WhenNewItemInstance, // WhenItemFocus → closest
                308 => TriggerType.PostTextItem,        // WhenItemBlur → closest

                // Navigation
                400 => TriggerType.KeyPreviousRecord,   // PreRecordNavigate
                401 => TriggerType.KeyNextRecord,        // PostRecordNavigate

                // Error
                500 => TriggerType.OnError,
                501 => TriggerType.OnMessage,

                // Extra
                600 => TriggerType.OnRollback,           // PreBlockRollback

                _ => TriggerType.UserNamed
            };
        }

        /// <summary>
        /// Wrap a local async bool handler into a BeepDM TriggerDefinition.
        /// </summary>
        public static TriggerDefinition ToBeepDMTrigger(
            string blockName,
            string triggerName,
            int localTriggerType,
            Func<Task<bool>> localHandler,
            int executionOrder = 0,
            string description = null)
        {
            var dmType = MapTriggerType(localTriggerType);

            return new TriggerDefinition(dmType, TriggerScope.Block)
            {
                BlockName = blockName,
                TriggerName = triggerName ?? $"{blockName}_{dmType}_{Guid.NewGuid():N}".Substring(0, 40),
                Description = description,
                Priority = MapPriority(executionOrder),
                IsEnabled = true,
                AsyncHandler = async (ctx, ct) =>
                {
                    var ok = await localHandler().ConfigureAwait(false);
                    return ok ? TriggerResult.Success : TriggerResult.Failure;
                }
            };
        }

        private static TriggerPriority MapPriority(int executionOrder)
        {
            if (executionOrder <= -10) return TriggerPriority.Highest;
            if (executionOrder < 0) return TriggerPriority.High;
            if (executionOrder == 0) return TriggerPriority.Normal;
            if (executionOrder < 10) return TriggerPriority.Low;
            return TriggerPriority.Lowest;
        }
    }

    /// <summary>
    /// Bridges WinForms-local LOV definitions to the BeepDM ILOVManager API.
    /// The LOV types are structurally very similar — this is mostly a property copy.
    /// </summary>
    public static class LOVBridge
    {
        /// <summary>
        /// Convert a local BeepDataBlockLOV-style registration to a BeepDM LOVDefinition.
        /// </summary>
        public static LOVDefinition ToBeepDMLOV(
            string lovName,
            string title,
            string dataSourceName,
            string entityName,
            string displayField,
            string returnField,
            string whereClause,
            string orderByClause,
            bool allowSearch,
            int searchMode,
            int width,
            int height,
            bool allowMultiSelect,
            bool autoRefresh,
            int validationType,
            bool autoDisplay,
            int autoDisplayMinChars,
            bool autoPopulateRelatedFields,
            Dictionary<string, string> relatedFieldMappings,
            bool useCache,
            int cacheDurationMinutes,
            List<AppFilter> filters,
            List<LOVColumnInfo> columns)
        {
            var lov = new LOVDefinition
            {
                LOVName = lovName,
                Title = title,
                DataSourceName = dataSourceName,
                EntityName = entityName,
                DisplayField = displayField,
                ReturnField = returnField ?? displayField,
                WhereClause = whereClause,
                OrderByClause = orderByClause,
                AllowSearch = allowSearch,
                SearchMode = (LOVSearchMode)searchMode,
                Width = width,
                Height = height,
                AllowMultiSelect = allowMultiSelect,
                AutoRefresh = autoRefresh,
                ValidationType = (LOVValidationType)validationType,
                AutoDisplay = autoDisplay,
                AutoDisplayMinChars = autoDisplayMinChars,
                AutoPopulateRelatedFields = autoPopulateRelatedFields,
                RelatedFieldMappings = relatedFieldMappings ?? new Dictionary<string, string>(),
                UseCache = useCache,
                CacheDurationMinutes = cacheDurationMinutes,
                Filters = filters ?? new List<AppFilter>()
            };

            if (columns != null)
            {
                foreach (var col in columns)
                {
                    lov.Columns.Add(new LOVColumn
                    {
                        FieldName = col.FieldName,
                        DisplayName = col.DisplayName,
                        Width = col.Width,
                        Visible = col.Visible,
                        Searchable = col.Searchable,
                        Format = col.Format,
                        Alignment = (LOVColumnAlignment)col.Alignment
                    });
                }
            }

            return lov;
        }
    }

    /// <summary>
    /// Lightweight column info DTO used by LOVBridge to avoid referencing WinForms LOVColumn type.
    /// </summary>
    public class LOVColumnInfo
    {
        /// <summary>Gets or sets the source field name.</summary>
        public string FieldName { get; set; }

        /// <summary>Gets or sets the display caption.</summary>
        public string DisplayName { get; set; }

        /// <summary>Gets or sets the preferred column width.</summary>
        public int Width { get; set; } = 100;

        /// <summary>Gets or sets whether the column is visible.</summary>
        public bool Visible { get; set; } = true;

        /// <summary>Gets or sets whether the column can participate in search.</summary>
        public bool Searchable { get; set; } = true;

        /// <summary>Gets or sets the optional display format string.</summary>
        public string Format { get; set; }

        /// <summary>Gets or sets the column alignment. 0=Left, 1=Center, 2=Right.</summary>
        public int Alignment { get; set; } // 0=Left, 1=Center, 2=Right
    }
}
