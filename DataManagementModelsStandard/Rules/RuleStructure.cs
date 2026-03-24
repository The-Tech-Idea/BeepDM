using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Rules
{
    public partial class RuleStructure : Entity, IRuleStructure
    {
        public const string CurrentSchemaVersion = "1.0";

        public RuleStructure()
        {
            GuidID = Guid.NewGuid().ToString();
            SchemaVersion = CurrentSchemaVersion;
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
            Tokens = new List<Token>();
            LifecycleState = RuleLifecycleState.Draft;
        }

        private int _id;
        public int ID
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _guidid;
        public string GuidID
        {
            get => _guidid;
            set => SetProperty(ref _guidid, value);
        }

        private string _rulename;
        public string Rulename
        {
            get => _rulename;
            set => SetProperty(ref _rulename, value);
        }

        private string _ruletype;
        public string RuleType
        {
            get => _ruletype;
            set => SetProperty(ref _ruletype, value);
        }

        private string _expression;
        public string Expression
        {
            get => _expression;
            set => SetProperty(ref _expression, value);
        }

        private List<Token> _tokens;
        public List<Token> Tokens
        {
            get => _tokens;
            set => SetProperty(ref _tokens, value);
        }

        public void Touch() => UpdatedUtc = DateTime.UtcNow;
    }
}
