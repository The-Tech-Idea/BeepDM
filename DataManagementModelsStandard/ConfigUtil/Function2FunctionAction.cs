using System;

using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.ConfigUtil
{
    public class Function2FunctionAction : Entity
    {
        public Function2FunctionAction()
        {
            GuidID = Guid.NewGuid().ToString();
        }

        private int _id;
        public int ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }
        private AddinType _fromType;
        public AddinType FromType
        {
            get { return _fromType; }
            set { SetProperty(ref _fromType, value); }
        }
        private AddinType _toType;
        public AddinType ToType
        {
            get { return _toType; }
            set { SetProperty(ref _toType, value); }
        }
        private string _guidid;
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        }

        private string _actiontype;
        public string ActionType
        {
            get { return _actiontype; }
            set { SetProperty(ref _actiontype, value); }
        }

        private string _event;
        public string Event
        {
            get { return _event; }
            set { SetProperty(ref _event, value); }
        }

        private string _fromclass;
        public string FromClass
        {
            get { return _fromclass; }
            set { SetProperty(ref _fromclass, value); }
        }

        private string _frommethod;
        public string FromMethod
        {
            get { return _frommethod; }
            set { SetProperty(ref _frommethod, value); }
        }

        private string _toclass;
        public string ToClass
        {
            get { return _toclass; }
            set { SetProperty(ref _toclass, value); }
        }

        private string _tomethod;
        public string ToMethod
        {
            get { return _tomethod; }
            set { SetProperty(ref _tomethod, value); }
        }

        private int _fromid;
        public int FromID
        {
            get { return _fromid; }
            set { SetProperty(ref _fromid, value); }
        }

        private int _toid;
        public int ToID
        {
            get { return _toid; }
            set { SetProperty(ref _toid, value); }
        }

        private string _param1;
        public string Param1
        {
            get { return _param1; }
            set { SetProperty(ref _param1, value); }
        }

        private string _param2;
        public string Param2
        {
            get { return _param2; }
            set { SetProperty(ref _param2, value); }
        }

        private string _param3;
        public string Param3
        {
            get { return _param3; }
            set { SetProperty(ref _param3, value); }
        }

        private string _param4;
        public string Param4
        {
            get { return _param4; }
            set { SetProperty(ref _param4, value); }
        }

        private string _param5;
        public string Param5
        {
            get { return _param5; }
            set { SetProperty(ref _param5, value); }
        }

        private string _param6;
        public string Param6
        {
            get { return _param6; }
            set { SetProperty(ref _param6, value); }
        }
    }

}
