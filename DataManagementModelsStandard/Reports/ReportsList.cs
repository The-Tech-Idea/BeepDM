using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Report
{
    public class ReportsList : Entity
    {
        public ReportsList()
        {

        }

        private int _id;
        public int ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _guidid = Guid.NewGuid().ToString();
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        } 

        private string _reportname;
        public string ReportName
        {
            get { return _reportname; }
            set { SetProperty(ref _reportname, value); }
        }

        private string _reportdefinition;
        public string ReportDefinition
        {
            get { return _reportdefinition; }
            set { SetProperty(ref _reportdefinition, value); }
        }

        private string _reportengine;
        public string ReportEngine
        {
            get { return _reportengine; }
            set { SetProperty(ref _reportengine, value); }
        }

    }
}
