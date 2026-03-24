using System.Collections.Generic;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Editor.Defaults.Interfaces
{
    /// <summary>
    /// Enriched resolution context passed through the resolver pipeline.
    /// Wraps <see cref="IPassedArgs"/> and carries per-call extras so resolvers
    /// never need to cast the raw args to reach entity/field names.
    /// </summary>
    public class ResolverContext
    {
        /// <summary>Original framework arguments.</summary>
        public IPassedArgs PassedArgs { get; }

        /// <summary>Caller-supplied extra key/value pairs (e.g. FieldName, EntityName overrides).</summary>
        public Dictionary<string, object> Extras { get; }

        /// <summary>Convenience shortcut — current entity from <see cref="IPassedArgs"/>.</summary>
        public string EntityName => PassedArgs?.CurrentEntity;

        /// <summary>
        /// Field name resolved from Extras["FieldName"] first, then
        /// <see cref="IPassedArgs.ParameterString1"/> as fallback.
        /// </summary>
        public string FieldName =>
            Extras != null && Extras.TryGetValue("FieldName", out var fn) ? fn?.ToString()
            : PassedArgs?.ParameterString1;

        public ResolverContext(IPassedArgs passedArgs)
        {
            PassedArgs = passedArgs;
            Extras     = new Dictionary<string, object>();
        }

        public ResolverContext(IPassedArgs passedArgs, Dictionary<string, object> extras)
        {
            PassedArgs = passedArgs;
            Extras     = extras ?? new Dictionary<string, object>();
        }

        /// <summary>Returns the extra value cast to <typeparamref name="T"/>, or the default when absent.</summary>
        public T GetExtra<T>(string key, T defaultValue = default)
        {
            if (Extras == null || !Extras.TryGetValue(key, out var raw))
                return defaultValue;
            return raw is T typed ? typed : defaultValue;
        }
    }
}
