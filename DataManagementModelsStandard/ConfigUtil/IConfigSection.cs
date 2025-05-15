using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.ConfigUtil
{
    public interface IConfigSection
    {
        void Load();
        void Save();
    }

    public class ConnectionsConfigSection : IConfigSection
    {
        private readonly IConfigEditor _editor;

        public ConnectionsConfigSection(IConfigEditor editor)
        {
            _editor = editor;
        }

        public void Load() { /* Load connections */ }
        public void Save() { /* Save connections */ }
    }
}
