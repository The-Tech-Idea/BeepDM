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
    public enum RoundingStyle
    {
        Circle,Ellipse
    }
    public class RoundButton: Button
    {
        private RoundingStyle _RoundingStyle = RoundingStyle.Ellipse;
        public RoundingStyle RoundingStyle
        {
            get { return _RoundingStyle; }
            set
            {
                _RoundingStyle = value;
                if (value == RoundingStyle.Ellipse)
                {
                    this.Invalidate();
                  
                }
                
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
        protected override void OnPaint(PaintEventArgs e)
        {
            if (RoundingStyle== RoundingStyle.Circle)
            {
                GraphicsPath gp = new GraphicsPath();
                gp.AddEllipse(0, 0, ClientSize.Width, ClientSize.Height);

                this.Region = new Region(gp);
            }
            else
            {
                Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, _CornerRadius, _CornerRadius));
            }
           

            base.OnPaint(e);

        }
        protected override void OnSizeChanged(EventArgs e)
        {
            //  Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, _CornerRadius, _CornerRadius));
            this.Invalidate();
            base.OnSizeChanged(e);
        }
        public int CornerRadius
        {
            get { return _CornerRadius; }
            set
            {
                _CornerRadius = value;

              //  Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, _CornerRadius, _CornerRadius));
                this.Invalidate();
            }
        }
    }
}
