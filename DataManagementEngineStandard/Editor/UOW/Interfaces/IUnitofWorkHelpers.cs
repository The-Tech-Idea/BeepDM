using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOW.Interfaces
{
    /// <summary>
    /// Interface for resolving default values using rules and formulas
    /// </summary>
    public interface IDefaultValueResolver
    {
        /// <summary>
        /// Gets the name of the resolver
        /// </summary>
        string ResolverName { get; }

        /// <summary>
        /// Gets the supported rule types for this resolver
        /// </summary>
        IEnumerable<string> SupportedRuleTypes { get; }

        /// <summary>
        /// Resolves a default value based on the rule and parameters
        /// </summary>
        /// <param name="rule">The rule string to resolve</param>
        /// <param name="parameters">Parameters for rule resolution</param>
        /// <returns>The resolved value</returns>
        object ResolveValue(string rule, IPassedArgs parameters);

        /// <summary>
        /// Validates if the rule can be handled by this resolver
        /// </summary>
        /// <param name="rule">The rule string to validate</param>
        /// <returns>True if the rule can be handled</returns>
        bool CanHandle(string rule);

        /// <summary>
        /// Gets example usage for this resolver
        /// </summary>
        /// <returns>List of example rule strings</returns>
        IEnumerable<string> GetExamples();
    }

    /// <summary>
    /// Interface for default value operations in UnitofWork
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IUnitofWorkDefaults<T> where T : Entity, new()
    {
        /// <summary>
        /// Applies default values to an entity
        /// </summary>
        /// <param name="entity">Entity to apply defaults to</param>
        /// <param name="context">Context for default value resolution</param>
        void ApplyDefaults(T entity, DefaultValueContext context);

        /// <summary>
        /// Applies default values to an entity asynchronously
        /// </summary>
        /// <param name="entity">Entity to apply defaults to</param>
        /// <param name="context">Context for default value resolution</param>
        /// <returns>Entity with defaults applied</returns>
        Task<T> ApplyDefaultsAsync(T entity, DefaultValueContext context);

        /// <summary>
        /// Checks if defaults are configured for a specific field
        /// </summary>
        /// <param name="fieldName">Name of the field to check</param>
        /// <returns>True if defaults exist for the field</returns>
        bool HasDefaults(string fieldName);

        /// <summary>
        /// Gets the default value configuration for a specific field
        /// </summary>
        /// <param name="fieldName">Name of the field</param>
        /// <returns>Default value configuration or null if not found</returns>
        DefaultValue GetDefaultForField(string fieldName);

        /// <summary>
        /// Applies defaults for insert operations
        /// </summary>
        /// <param name="entity">Entity being inserted</param>
        void ApplyInsertDefaults(T entity);

        /// <summary>
        /// Applies defaults for update operations
        /// </summary>
        /// <param name="entity">Entity being updated</param>
        void ApplyUpdateDefaults(T entity);

        /// <summary>
        /// Validates that applied defaults meet entity constraints
        /// </summary>
        /// <param name="entity">Entity with applied defaults</param>
        /// <returns>Validation result</returns>
        IErrorsInfo ValidateAppliedDefaults(T entity);
    }

    /// <summary>
    /// Interface for validation operations in UnitofWork
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IUnitofWorkValidation<T> where T : Entity, new()
    {
        /// <summary>
        /// Validates an entity
        /// </summary>
        /// <param name="entity">Entity to validate</param>
        /// <returns>Validation result</returns>
        IErrorsInfo ValidateEntity(T entity);

        /// <summary>
        /// Validates an entity for insert operation
        /// </summary>
        /// <param name="entity">Entity to validate</param>
        /// <returns>Validation result</returns>
        IErrorsInfo ValidateForInsert(T entity);

        /// <summary>
        /// Validates an entity for update operation
        /// </summary>
        /// <param name="entity">Entity to validate</param>
        /// <returns>Validation result</returns>
        IErrorsInfo ValidateForUpdate(T entity);

        /// <summary>
        /// Validates an entity for delete operation
        /// </summary>
        /// <param name="entity">Entity to validate</param>
        /// <returns>Validation result</returns>
        IErrorsInfo ValidateForDelete(T entity);

        /// <summary>
        /// Validates primary key for an entity
        /// </summary>
        /// <param name="entity">Entity to validate</param>
        /// <returns>Validation result</returns>
        IErrorsInfo ValidatePrimaryKey(T entity);

        /// <summary>
        /// Validates required fields for an entity
        /// </summary>
        /// <param name="entity">Entity to validate</param>
        /// <returns>Validation result</returns>
        IErrorsInfo ValidateRequiredFields(T entity);
    }

    /// <summary>
    /// Interface for data operations helper in UnitofWork
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IUnitofWorkDataHelper<T> where T : Entity, new()
    {
        /// <summary>
        /// Clones an entity
        /// </summary>
        /// <param name="entity">Entity to clone</param>
        /// <returns>Cloned entity</returns>
        T CloneEntity(T entity);

        /// <summary>
        /// Converts an object to the entity type
        /// </summary>
        /// <param name="source">Source object</param>
        /// <returns>Converted entity</returns>
        T ConvertToEntity(object source);

        /// <summary>
        /// Gets entity values as dictionary
        /// </summary>
        /// <param name="entity">Entity to extract values from</param>
        /// <returns>Dictionary of property names and values</returns>
        Dictionary<string, object> GetEntityValues(T entity);

        /// <summary>
        /// Sets entity values from dictionary
        /// </summary>
        /// <param name="entity">Entity to set values on</param>
        /// <param name="values">Dictionary of property names and values</param>
        void SetEntityValues(T entity, Dictionary<string, object> values);

        /// <summary>
        /// Compares two entities for changes
        /// </summary>
        /// <param name="original">Original entity</param>
        /// <param name="current">Current entity</param>
        /// <returns>Dictionary of changed fields</returns>
        Dictionary<string, (object oldValue, object newValue)> CompareEntities(T original, T current);
    }

    /// <summary>
    /// Interface for state management in UnitofWork
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IUnitofWorkStateHelper<T> where T : Entity, new()
    {
        /// <summary>
        /// Gets the current state of an entity
        /// </summary>
        /// <param name="entity">Entity to check</param>
        /// <returns>Entity state</returns>
        EntityState GetEntityState(T entity);

        /// <summary>
        /// Sets the state of an entity
        /// </summary>
        /// <param name="entity">Entity to set state for</param>
        /// <param name="state">New state</param>
        void SetEntityState(T entity, EntityState state);

        /// <summary>
        /// Marks entity for deletion
        /// </summary>
        /// <param name="entity">Entity to mark for deletion</param>
        void MarkForDeletion(T entity);

        /// <summary>
        /// Marks entity as modified
        /// </summary>
        /// <param name="entity">Entity to mark as modified</param>
        void MarkAsModified(T entity);

        /// <summary>
        /// Marks entity as added
        /// </summary>
        /// <param name="entity">Entity to mark as added</param>
        void MarkAsAdded(T entity);

        /// <summary>
        /// Resets entity state to unchanged
        /// </summary>
        /// <param name="entity">Entity to reset</param>
        void ResetState(T entity);
    }

    /// <summary>
    /// Interface for event management in UnitofWork
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IUnitofWorkEventHelper<T> where T : Entity, new()
    {
        /// <summary>
        /// Creates event parameters for UnitofWork operations
        /// </summary>
        /// <param name="entity">Entity involved</param>
        /// <param name="action">Event action</param>
        /// <returns>Event parameters</returns>
        UnitofWorkParams CreateEventParams(T entity, EventAction action);

        /// <summary>
        /// Handles property changed events
        /// </summary>
        /// <param name="entity">Entity that changed</param>
        /// <param name="propertyName">Name of changed property</param>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        void HandlePropertyChanged(T entity, string propertyName, object oldValue, object newValue);

        /// <summary>
        /// Raises appropriate events for entity operations
        /// </summary>
        /// <param name="entity">Entity involved</param>
        /// <param name="action">Action being performed</param>
        /// <param name="isPreEvent">True for pre-events, false for post-events</param>
        /// <returns>True if operation should continue</returns>
        bool RaiseEntityEvent(T entity, EventAction action, bool isPreEvent);
    }

    /// <summary>
    /// Interface for collection management in UnitofWork
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IUnitofWorkCollectionHelper<T> where T : Entity, new()
    {
        /// <summary>
        /// Synchronizes collections
        /// </summary>
        /// <param name="source">Source collection</param>
        /// <param name="target">Target collection</param>
        void SynchronizeCollections(ObservableBindingList<T> source, ObservableBindingList<T> target);

        /// <summary>
        /// Filters collection based on criteria
        /// </summary>
        /// <param name="collection">Collection to filter</param>
        /// <param name="filters">Filter criteria</param>
        /// <returns>Filtered collection</returns>
        ObservableBindingList<T> FilterCollection(ObservableBindingList<T> collection, List<AppFilter> filters);

        /// <summary>
        /// Applies paging to collection
        /// </summary>
        /// <param name="collection">Collection to page</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paged collection</returns>
        ObservableBindingList<T> ApplyPaging(ObservableBindingList<T> collection, int pageIndex, int pageSize);

        /// <summary>
        /// Sorts collection
        /// </summary>
        /// <param name="collection">Collection to sort</param>
        /// <param name="sortField">Field to sort by</param>
        /// <param name="ascending">Sort direction</param>
        /// <returns>Sorted collection</returns>
        ObservableBindingList<T> SortCollection(ObservableBindingList<T> collection, string sortField, bool ascending);
    }

    /// <summary>
    /// Context for default value resolution
    /// </summary>
    public class DefaultValueContext
    {
        /// <summary>
        /// Operation being performed (Insert, Update, etc.)
        /// </summary>
        public string Operation { get; set; }

        /// <summary>
        /// Data source name
        /// </summary>
        public string DataSourceName { get; set; }

        /// <summary>
        /// Entity name
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// User context information
        /// </summary>
        public string UserContext { get; set; }

        /// <summary>
        /// Additional parameters for default resolution
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Whether this is a new entity
        /// </summary>
        public bool IsNewEntity { get; set; }

        /// <summary>
        /// Timestamp of the operation
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}