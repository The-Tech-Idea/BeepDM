using System;

using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.ConfigUtil
{
    public class Event : Entity
    {
        public Event()
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

        private string _eventname;
        public string EventName
        {
            get { return _eventname; }
            set { SetProperty(ref _eventname, value); }
        }
    }

}
