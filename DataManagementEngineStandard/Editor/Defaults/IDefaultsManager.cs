using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.Defaults.Interfaces;

namespace TheTechIdea.Beep.Editor.Defaults
{
    public interface IDefaultsManager
    {
        static abstract IDefaultValueHelper DefaultValueHelper { get; }
        static abstract IDefaultValueResolverManager ResolverManager { get; }
        static abstract IDefaultValueValidationHelper ValidationHelper { get; }

        static abstract object ApplyDefaultsToRecord(IDMEEditor editor, string dataSourceName, string entityName, object record, IPassedArgs parameters = null);
        static abstract (IErrorsInfo validation, DefaultValue defaultValue) CreateDefaultValue(IDMEEditor editor, string fieldName, string value, string rule = null);
        static abstract List<DefaultValue> CreateDefaultValueTemplate(IDMEEditor editor, DefaultValueTemplateType templateType);
        static abstract string ExportDefaults(IDMEEditor editor, string dataSourceName);
        static abstract Dictionary<string, IEnumerable<string>> GetAvailableResolvers(IDMEEditor editor);
        static abstract object GetColumnDefault(IDMEEditor editor, string dataSourceName, string entityName, string columnName, IPassedArgs parameters = null);
        static abstract List<DefaultValue> GetDefaults(IDMEEditor editor, string dataSourceName);
        static abstract Dictionary<string, DefaultValue> GetEntityDefaults(IDMEEditor editor, string dataSourceName, string entityName);
        static abstract Dictionary<string, IEnumerable<string>> GetResolverExamples(IDMEEditor editor);
        static abstract IErrorsInfo ImportDefaults(IDMEEditor editor, string dataSourceName, string serializedDefaults, bool replaceExisting = false);
        static abstract void Initialize(IDMEEditor editor);
        static abstract void RegisterCustomResolver(IDMEEditor editor, IDefaultValueResolver resolver);
        static abstract IErrorsInfo RemoveColumnDefault(IDMEEditor editor, string dataSourceName, string entityName, string columnName);
        static abstract object ResolveDefaultValue(IDMEEditor editor, DefaultValue defaultValue, IPassedArgs parameters);
        static abstract object ResolveDefaultValue(IDMEEditor editor, string dataSourceName, string fieldName, IPassedArgs parameters);
        static abstract IErrorsInfo SaveDefaults(IDMEEditor editor, List<DefaultValue> defaults, string dataSourceName);
        static abstract IErrorsInfo SetColumnDefault(IDMEEditor editor, string dataSourceName, string entityName, string columnName, string defaultValue, bool isRule = false);
        static abstract IErrorsInfo SetMultipleColumnDefaults(IDMEEditor editor, string dataSourceName, string entityName, Dictionary<string, (string value, bool isRule)> columnDefaults);
        static abstract (IErrorsInfo result, object value) TestRule(IDMEEditor editor, string rule, IPassedArgs parameters = null);
        static abstract IErrorsInfo ValidateDefaultValue(IDMEEditor editor, DefaultValue defaultValue);
        static abstract IErrorsInfo ValidateRule(IDMEEditor editor, string rule);
        void Dispose();
    }
}