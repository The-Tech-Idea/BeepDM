using System;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.ConfigUtil
{
    /// <summary>
    /// Represents a default value configuration for entity properties with support for static values and dynamic rules
    /// </summary>
    public class DefaultValue : Entity
    {
        public DefaultValue()
        {
            GuidID = Guid.NewGuid().ToString();
            propertyType = DefaultValueType.Static; // Set reasonable default
        }

        private int _id;
        /// <summary>
        /// Unique numeric identifier for the default value
        /// </summary>
        public int ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _guidid;
        /// <summary>
        /// Unique GUID identifier for the default value
        /// </summary>
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        }

        private string _propertyname;
        /// <summary>
        /// Name of the property/field this default value applies to
        /// </summary>
        public string PropertyName
        {
            get { return _propertyname; }
            set { SetProperty(ref _propertyname, value); }
        }

        private object _propertyvalue; // Changed from string to object for better type support
        /// <summary>
        /// Static default value to use when propertyType is Static, ReplaceValue, or similar
        /// </summary>
        public object PropertyValue
        {
            get { return _propertyvalue; }
            set { SetProperty(ref _propertyvalue, value); }
        }

        private string _rule;
        /// <summary>
        /// Rule or expression to use for dynamic value generation when propertyType is Rule, Expression, Function, etc.
        /// </summary>
        public string Rule
        {
            get { return _rule; }
            set { SetProperty(ref _rule, value); }
        }

        private DefaultValueType _propertytype;
        /// <summary>
        /// Type of default value processing to apply (Static, Rule, Expression, Function, etc.)
        /// </summary>
        public DefaultValueType propertyType
        {
            get { return _propertytype; }
            set { SetProperty(ref _propertytype, value); }
        }

        private string _propertycategory;
        /// <summary>
        /// Optional category for grouping related default values
        /// </summary>
        public string PropertyCategory
        {
            get { return _propertycategory; }
            set { SetProperty(ref _propertycategory, value); }
        }

        private string _description;
        /// <summary>
        /// Optional description explaining the purpose of this default value
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { SetProperty(ref _description, value); }
        }

        private bool _isEnabled = true;
        /// <summary>
        /// Whether this default value is currently enabled/active
        /// </summary>
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetProperty(ref _isEnabled, value); }
        }

        private DateTime _createdDate = DateTime.Now;
        /// <summary>
        /// When this default value configuration was created
        /// </summary>
        public DateTime CreatedDate
        {
            get { return _createdDate; }
            set { SetProperty(ref _createdDate, value); }
        }

        private DateTime _modifiedDate = DateTime.Now;
        /// <summary>
        /// When this default value configuration was last modified
        /// </summary>
        public DateTime ModifiedDate
        {
            get { return _modifiedDate; }
            set { SetProperty(ref _modifiedDate, value); }
        }

        /// <summary>
        /// Gets whether this default value uses a rule (dynamic) vs static value
        /// </summary>
        public bool IsRuleBased => !string.IsNullOrEmpty(Rule) && 
            (propertyType == DefaultValueType.Rule || 
             propertyType == DefaultValueType.Expression || 
             propertyType == DefaultValueType.Function ||
             propertyType == DefaultValueType.Computed ||
             propertyType == DefaultValueType.Conditional ||
             propertyType == DefaultValueType.Custom ||
             propertyType == DefaultValueType.Template ||
             IsAdvancedRuleType());

        /// <summary>
        /// Checks if the property type is one of the advanced rule types
        /// </summary>
        private bool IsAdvancedRuleType()
        {
            return propertyType == DefaultValueType.EntityLookup ||
                   propertyType == DefaultValueType.EnvironmentVariable ||
                   propertyType == DefaultValueType.ConfigurationValue ||
                   propertyType == DefaultValueType.SessionValue ||
                   propertyType == DefaultValueType.CachedValue ||
                   propertyType == DefaultValueType.ParentValue ||
                   propertyType == DefaultValueType.ChildValue ||
                   propertyType == DefaultValueType.WebService ||
                   propertyType == DefaultValueType.StoredProcedure ||
                   propertyType == DefaultValueType.RandomValue ||
                   propertyType == DefaultValueType.RoleBasedValue ||
                   propertyType == DefaultValueType.LocalizedValue ||
                   propertyType == DefaultValueType.BusinessCalendar ||
                   propertyType == DefaultValueType.UserPreference ||
                   propertyType == DefaultValueType.AuditValue ||
                   propertyType == DefaultValueType.LocationBased ||
                   propertyType == DefaultValueType.MLPrediction ||
                   propertyType == DefaultValueType.Statistical ||
                   propertyType == DefaultValueType.InheritedValue ||
                   propertyType == DefaultValueType.WorkflowContext;
        }

        /// <summary>
        /// Gets whether this default value is a simple static value
        /// </summary>
        public bool IsStatic => !IsRuleBased && 
            (propertyType == DefaultValueType.Static || 
             propertyType == DefaultValueType.ReplaceValue ||
             propertyType == DefaultValueType.DisplayLookup ||
             propertyType == DefaultValueType.Mapping);

        /// <summary>
        /// Validates the default value configuration
        /// </summary>
        /// <returns>True if configuration is valid</returns>
        public bool IsValid()
        {
            // Must have a property name
            if (string.IsNullOrWhiteSpace(PropertyName))
                return false;

            // Rule-based types must have a rule
            if (IsRuleBased && string.IsNullOrWhiteSpace(Rule))
                return false;

            // Static types should have a property value (null is acceptable for some cases)
            if (IsStatic && PropertyValue == null && 
                propertyType != DefaultValueType.DisplayLookup && 
                propertyType != DefaultValueType.Mapping)
                return false;

            return true;
        }

        /// <summary>
        /// Gets a human-readable description of this default value configuration
        /// </summary>
        /// <returns>Description string</returns>
        public override string ToString()
        {
            var result = $"{PropertyName}: ";
            
            if (IsRuleBased)
            {
                result += $"{propertyType} rule '{Rule}'";
            }
            else
            {
                result += $"{propertyType} value '{PropertyValue}'";
            }

            if (!IsEnabled)
            {
                result += " (DISABLED)";
            }

            return result;
        }

        /// <summary>
        /// Creates a copy of this default value configuration
        /// </summary>
        /// <returns>New DefaultValue instance with copied values</returns>
        public DefaultValue Clone()
        {
            return new DefaultValue
            {
                ID = this.ID,
                GuidID = Guid.NewGuid().ToString(), // Generate new GUID for clone
                PropertyName = this.PropertyName,
                PropertyValue = this.PropertyValue,
                Rule = this.Rule,
                propertyType = this.propertyType,
                PropertyCategory = this.PropertyCategory,
                Description = this.Description,
                IsEnabled = this.IsEnabled,
                CreatedDate = DateTime.Now, // Set new creation date
                ModifiedDate = DateTime.Now
            };
        }
    }
}
