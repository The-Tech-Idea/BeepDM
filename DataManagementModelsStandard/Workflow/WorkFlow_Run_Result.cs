using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Workflow
{
    public class WorkFlow_Run_Result : Entity
    {
        public WorkFlow_Run_Result()
        {
            StepsResult = new List<Workflow_Step_Run_result>();
            GuidID = Guid.NewGuid().ToString();
        }


        private string _guidid;
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        }


        private string _workflow_name;
        public string Workflow_Name
        {
            get { return _workflow_name; }
            set { SetProperty(ref _workflow_name, value); }
        }

        private DateTime _runtime;
        public DateTime RunTime
        {
            get { return _runtime; }
            set { SetProperty(ref _runtime, value); }
        }
        private List<Workflow_Step_Run_result> _stepsResult;
        public List<Workflow_Step_Run_result> StepsResult
        {
            get { return _stepsResult; }
            set { SetProperty(ref _stepsResult, value); }
        }
    }

    public class Workflow_Step_Run_result : Entity
    {
        public Workflow_Step_Run_result()
        {
            Ok = false;
            GuidID = Guid.NewGuid().ToString();
        }


        private string _guidid;
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        }

        private string _id;
        public string ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _stepname;
        public string StepName
        {
            get { return _stepname; }
            set { SetProperty(ref _stepname, value); }
        }

        private DateTime _runtime;
        public DateTime RunTime
        {
            get { return _runtime; }
            set { SetProperty(ref _runtime, value); }
        }

        private string _actionname;
        public string ActionName
        {
            get { return _actionname; }
            set { SetProperty(ref _actionname, value); }
        }

        private string _stepdescription;
        public string StepDescription
        {
            get { return _stepdescription; }
            set { SetProperty(ref _stepdescription, value); }
        }

        private string _errordescription;
        public string ErrorDescription
        {
            get { return _errordescription; }
            set { SetProperty(ref _errordescription, value); }
        }

        private string _actionlogfile;
        public string ActionLogFile
        {
            get { return _actionlogfile; }
            set { SetProperty(ref _actionlogfile, value); }
        }


        private bool _ok;
        public bool Ok
        {
            get { return _ok; }
            set { SetProperty(ref _ok, value); }
        }

    }

}

