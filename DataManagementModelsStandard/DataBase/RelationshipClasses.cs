using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.DataBase
{
   
    public class RelationShipKeys : Entity, IRelationShipKeys
    {

        private string _ralationname;
        public string RalationName
        {
            get { return _ralationname; }
            set { SetProperty(ref _ralationname, value); }
        }

        private string _relatedentityid;
        public string RelatedEntityID
        {
            get { return _relatedentityid; }
            set { SetProperty(ref _relatedentityid, value); }
        }

        private string _relatedentitycolumnid;
        public string RelatedEntityColumnID
        {
            get { return _relatedentitycolumnid; }
            set { SetProperty(ref _relatedentitycolumnid, value); }
        }

        private int _relatedcolumnsequenceid;
        public int RelatedColumnSequenceID
        {
            get { return _relatedcolumnsequenceid; }
            set { SetProperty(ref _relatedcolumnsequenceid, value); }
        }

        private string _entitycolumnid;
        public string EntityColumnID
        {
            get { return _entitycolumnid; }
            set { SetProperty(ref _entitycolumnid, value); }
        }

        private int _entitycolumnsequenceid;
        public int EntityColumnSequenceID
        {
            get { return _entitycolumnsequenceid; }
            set { SetProperty(ref _entitycolumnsequenceid, value); }
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
        public RelationShipKeys()
        {
            GuidID = Guid.NewGuid().ToString();
        }
        public RelationShipKeys(string pParentEntityID, string pParentEntityColumnID, string pEntityColumnID)
        {
            RelatedEntityID = pParentEntityID;
            RelatedEntityColumnID = pParentEntityColumnID;
            EntityColumnID = pEntityColumnID;

        }
    }
}
