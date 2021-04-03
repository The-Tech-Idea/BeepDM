using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace WinFormSharedProject.Controls
{
    public class RoundPanel :Panel
    {
        private Color _ColorTop = Color.AliceBlue;
        public Color ColorTop
        {
            get { return _ColorTop; }
            set
            {
                _ColorTop = value;

                this.Invalidate();
            }
        } 
        private Color _ColorBottom = Color.Azure;
        public Color ColorBottom
        {
            get { return _ColorBottom; }
            set
            {
                _ColorBottom = value;
                this.Invalidate();

            }
        } 
       
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
            (
               int nLeftRect,
               int nTopRect,
               int nRightRect,
               int nBottomRect,
               int nWidthEllipse,
               int nHeightEllipse
            );
      
        private int _CornerRadius = 30;

        public RoundPanel():base()
        {
            
        }
        protected override void OnPaint(PaintEventArgs pevent)
        {


            LinearGradientBrush lgb = new
            LinearGradientBrush(this.ClientRectangle, this.ColorTop,this.ColorBottom, 90F);
            Graphics g = pevent.Graphics;
            g.FillRectangle(lgb, this.ClientRectangle);


            base.OnPaint(pevent);
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, _CornerRadius, _CornerRadius));
            base.OnSizeChanged(e);
        }
        public int CornerRadius
        {
            get { return _CornerRadius; }
            set
            {
                _CornerRadius = value;
                
                    Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, _CornerRadius, _CornerRadius));
            }
        }

    }
}
