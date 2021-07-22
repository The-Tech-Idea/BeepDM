using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TheTechIdea.Beep.Report
{
    public partial class uc_webview : UserControl
    {
        public uc_webview()
        {
            InitializeComponent();
        }
        public void SetPage(string path)
        {
            webView21.NavigateToString(path);
        }
    }
}
