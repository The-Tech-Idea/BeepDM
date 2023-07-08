using System;

using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Workflow
{
    public class LoadDataLogResult : Entity
    {
        public LoadDataLogResult()
        {
            GuidID = Guid.NewGuid().ToString();
        }


        private string _guidid;
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        }

        private string _runid;
        public string RunID
        {
            get { return _runid; }
            set { SetProperty(ref _runid, value); }
        }

        private string _workflowid;
        public string WorkFlowID
        {
            get { return _workflowid; }
            set { SetProperty(ref _workflowid, value); }
        }

        private string _stepid;
        public string StepID
        {
            get { return _stepid; }
            set { SetProperty(ref _stepid, value); }
        }

        private DateTime _date;
        public DateTime Date
        {
            get { return _date; }
            set { SetProperty(ref _date, value); }
        }

        private string _errorinfomessege;
        public string ErrorInfoMessege
        {
            get { return _errorinfomessege; }
            set { SetProperty(ref _errorinfomessege, value); }
        }

        private string _errorinfocode;
        public string ErrorInfoCode
        {
            get { return _errorinfocode; }
            set { SetProperty(ref _errorinfocode, value); }
        }

        private int _rowindex;
        public int Rowindex
        {
            get { return _rowindex; }
            set { SetProperty(ref _rowindex, value); }
        }

        private string _rowid;
        public string RowID
        {
            get { return _rowid; }
            set { SetProperty(ref _rowid, value); }
        }

        private string _inputline;
        public string InputLine
        {
            get { return _inputline; }
            set { SetProperty(ref _inputline, value); }
        }



    }

}
