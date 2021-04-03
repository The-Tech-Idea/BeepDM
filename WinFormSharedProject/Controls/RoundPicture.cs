using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
namespace WinFormSharedProject.Controls
{
    public class RoundPicture :PictureBox
    {
        protected override void OnPaint(PaintEventArgs pevent)
        {

            GraphicsPath gp = new GraphicsPath();
            gp.AddEllipse(0, 0, ClientSize.Width, ClientSize.Height);

            this.Region = new Region(gp);

            base.OnPaint(pevent);
        }
    }
}
