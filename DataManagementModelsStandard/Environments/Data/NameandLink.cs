using System.Collections.Generic;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Container.Services
{
    public class NameandLink : Entity
    {
        public NameandLink() { }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private string _url;
        public string Url
        {
            get { return _url; }
            set { SetProperty(ref _url, value); }
        }

        private List<string> _parameters;
        public List<string> Parameters
        {
            get { return _parameters; }
            set { SetProperty(ref _parameters, value); }
        }
    }
}
