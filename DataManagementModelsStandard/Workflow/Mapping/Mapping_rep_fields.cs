using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Workflow
{
    public class Mapping_rep_fields : Entity, IMapping_rep_fields
    {
        public Mapping_rep_fields()
        {

            GuidID = Guid.NewGuid().ToString();

        }

        private string _guidid;
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        }

        private string _toentityname;
        public string ToEntityName
        {
            get { return _toentityname; }
            set { SetProperty(ref _toentityname, value); }
        }

        private string _tofieldname;
        public string ToFieldName
        {
            get { return _tofieldname; }
            set { SetProperty(ref _tofieldname, value); }
        }

        private string _tofieldtype;
        public string ToFieldType
        {
            get { return _tofieldtype; }
            set { SetProperty(ref _tofieldtype, value); }
        }

        private int _tofieldindex;
        public int ToFieldIndex
        {
            get { return _tofieldindex; }
            set { SetProperty(ref _tofieldindex, value); }
        }

        private string _fromentityname;
        public string FromEntityName
        {
            get { return _fromentityname; }
            set { SetProperty(ref _fromentityname, value); }
        }

        private string _fromfieldname;
        public string FromFieldName
        {
            get { return _fromfieldname; }
            set { SetProperty(ref _fromfieldname, value); }
        }

        private string _fromfieldtype;
        public string FromFieldType
        {
            get { return _fromfieldtype; }
            set { SetProperty(ref _fromfieldtype, value); }
        }

        private int _fromfieldindex;
        public int FromFieldIndex
        {
            get { return _fromfieldindex; }
            set { SetProperty(ref _fromfieldindex, value); }
        }

        private string _rules;
        public string Rules
        {
            get { return _rules; }
            set { SetProperty(ref _rules, value); }
        }

    }

}
