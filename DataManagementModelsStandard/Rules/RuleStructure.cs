using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Rules
{
    public class RuleStructure : Entity, IRuleStructure
    {
        public RuleStructure()
        {
            GuidID = Guid.NewGuid().ToString();
        }

        private int _id;
        public int ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _guidid;
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        }

        private string _rulename;
        public string Rulename
        {
            get { return _rulename; }
            set { SetProperty(ref _rulename, value); }
        }
        private string _ruletype;
        public string RuleType
        {
            get { return _ruletype; }
            set { SetProperty(ref _ruletype, value); }
        }

      

        private string _expression;
        public string Expression
        {
            get { return _expression; }
            set { SetProperty(ref _expression, value); }
        }
        private List<Token> _tokens;
        public List<Token> Tokens
        {
            get { return _tokens; }
            set { SetProperty(ref _tokens, value); }
        }
    }

}
