using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.AppBuilder
{
    public partial class uc_MainScreen : UserControl, IAppComponent
    {
        public uc_MainScreen()
        {
            InitializeComponent();
        }
        
        public string ComponentName { get ; set ; }
        public IDMEEditor DMEEditor { get; set; }
        public int DisplayOrder { get; set; } = 1;
        public AppComponentType ComponentType { get; set; } = AppComponentType.MainForm;
        
    }
}
