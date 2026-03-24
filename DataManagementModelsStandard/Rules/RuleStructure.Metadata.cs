using System;

namespace TheTechIdea.Beep.Rules
{
    public partial class RuleStructure
    {
        private string _schemaVersion;
        public string SchemaVersion
        {
            get => _schemaVersion;
            set => SetProperty(ref _schemaVersion, value);
        }

        private DateTime _createdUtc;
        public DateTime CreatedUtc
        {
            get => _createdUtc;
            set => SetProperty(ref _createdUtc, value);
        }

        private DateTime _updatedUtc;
        public DateTime UpdatedUtc
        {
            get => _updatedUtc;
            set => SetProperty(ref _updatedUtc, value);
        }

        private RuleLifecycleState _lifecycleState = RuleLifecycleState.Draft;
        public RuleLifecycleState LifecycleState
        {
            get => _lifecycleState;
            set => SetProperty(ref _lifecycleState, value);
        }

        private string _author;
        public string Author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }

        private string _tags;
        /// <summary>Comma-separated tags for catalog discovery queries.</summary>
        public string Tags
        {
            get => _tags;
            set => SetProperty(ref _tags, value);
        }

        private string _module;
        /// <summary>The module or subsystem this rule belongs to (for catalog grouping).</summary>
        public string Module
        {
            get => _module;
            set => SetProperty(ref _module, value);
        }

        /// <summary>
        /// Returns true when this rule's schema version is compatible with the supplied engine version.
        /// Currently uses a simple major-version equality check.
        /// </summary>
        public bool IsCompatibleVersion(string engineVersion)
        {
            if (string.IsNullOrEmpty(SchemaVersion) || string.IsNullOrEmpty(engineVersion))
                return false;

            var selfMajor = SchemaVersion.Split('.')[0];
            var engineMajor = engineVersion.Split('.')[0];
            return string.Equals(selfMajor, engineMajor, StringComparison.OrdinalIgnoreCase);
        }
    }
}
