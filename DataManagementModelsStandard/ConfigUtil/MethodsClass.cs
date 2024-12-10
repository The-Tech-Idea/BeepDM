using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.ConfigUtil
{
    public class MethodsClass : Entity
    {
        public MethodsClass()
        {
            GuidID = Guid.NewGuid().ToString();
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

        private MethodInfo _info;
        public MethodInfo Info
        {
            get { return _info; }
            set { SetProperty(ref _info, value); }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private string _caption;
        public string Caption
        {
            get { return _caption; }
            set { SetProperty(ref _caption, value); }
        }

        private bool _hidden;
        public bool Hidden
        {
            get { return _hidden; }
            set { SetProperty(ref _hidden, value); }
        }

        private bool _click = false;
        public bool Click
        {
            get { return _click; }
            set { SetProperty(ref _click, value); }
        }

        private Type _type;
        public Type type
        {
            get { return _type; }
            set { SetProperty(ref _type, value); }
        }

        private bool _doubleclick = false;
        public bool DoubleClick
        {
            get { return _doubleclick; }
            set { SetProperty(ref _doubleclick, value); }
        }

        private string _iconimage = null;
        public string iconimage
        {
            get { return _iconimage; }
            set { SetProperty(ref _iconimage, value); }
        }

        private EnumPointType _pointtype;
        public EnumPointType PointType
        {
            get { return _pointtype; }
            set { SetProperty(ref _pointtype, value); }
        }

        private string _objecttype = null;
        public string ObjectType
        {
            get { return _objecttype; }
            set { SetProperty(ref _objecttype, value); }
        }

        private string _classtype = null;
        public string ClassType
        {
            get { return _classtype; }
            set { SetProperty(ref _classtype, value); }
        }

        private string _misc = null;
        public string misc
        {
            get { return _misc; }
            set { SetProperty(ref _misc, value); }
        }

        private DatasourceCategory _category = DatasourceCategory.NONE;
        public DatasourceCategory Category
        {
            get { return _category; }
            set { SetProperty(ref _category, value); }
        }

        private DataSourceType _datasourcetype = DataSourceType.NONE;
        public DataSourceType DatasourceType
        {
            get { return _datasourcetype; }
            set { SetProperty(ref _datasourcetype, value); }
        }

        private ShowinType _showin = ShowinType.Both;
        public ShowinType Showin
        {
            get { return _showin; }
            set { SetProperty(ref _showin, value); }
        }

        private AddinAttribute _addinattr;
        public AddinAttribute AddinAttr
        {
            get { return _addinattr; }
            set { SetProperty(ref _addinattr, value); }
        }

        private CommandAttribute _commandattr;
        public CommandAttribute CommandAttr
        {
            get { return _commandattr; }
            set { SetProperty(ref _commandattr, value); }
        }
    }
}
