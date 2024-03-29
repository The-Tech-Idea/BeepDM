﻿using System;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Workflow
{
    public class ObjectTypes : Entity
    {
        public ObjectTypes()
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

        private string _objectname;
        public string ObjectName
        {
            get { return _objectname; }
            set { SetProperty(ref _objectname, value); }
        }

        private string _objecttype;
        public string ObjectType
        {
            get { return _objecttype; }
            set { SetProperty(ref _objecttype, value); }
        }

    }

}
