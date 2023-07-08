using System;

using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Workflow.Interfaces;

namespace TheTechIdea.Beep.Workflow
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

        private string _fieldname;
        public string Fieldname
        {
            get { return _fieldname; }
            set { SetProperty(ref _fieldname, value); }
        }

        private string _expression;
        public string Expression
        {
            get { return _expression; }
            set { SetProperty(ref _expression, value); }
        }
    }

}
