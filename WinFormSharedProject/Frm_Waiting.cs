using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TheTechIdea.Winforms.VIS
{
    public partial class Frm_Waiting : Form
    {
        public Frm_Waiting()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;

        }
        public Frm_Waiting(Form parent)
        {
            InitializeComponent();
            if (parent != null)
            {
                this.StartPosition = FormStartPosition.Manual;
                this.Location = new Point(parent.Location.X + parent.Width / 2 - this.Width / 2, parent.Location.Y + parent.Height / 2 - this.Height / 2);
            }
            else
            {
                this.StartPosition = FormStartPosition.CenterParent;
            }
            progressBar1.Style = ProgressBarStyle.Marquee;

        }
        public void CloseForm()
        {
            this.Close();
        }
        public void ChangeCaption(string newCaption)
        {
            this.Captionlabel.BeginInvoke(new Action(() =>
            {
                this.Captionlabel.AppendText(newCaption + Environment.NewLine);
            }));

            // Captionlabel.Text = newCaption;
        }
        public void AddComment(String comment)
        {
            this.DetailsTextBox.BeginInvoke(new Action(() =>
            {
                this.DetailsTextBox.AppendText(comment + Environment.NewLine);
            }));
        }

    }
}
