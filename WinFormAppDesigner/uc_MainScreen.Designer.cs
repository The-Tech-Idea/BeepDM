
namespace TheTechIdea.DataManagment_Engine.AppBuilder
{
    partial class uc_MainScreen
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.panel1 = new System.Windows.Forms.Panel();
            this.SubTitlelabel = new System.Windows.Forms.Label();
            this.Titlelabel = new System.Windows.Forms.Label();
            this.LogoPictureBox = new System.Windows.Forms.PictureBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.NavigationTreeView = new System.Windows.Forms.TreeView();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.roundPanel1 = new WinFormSharedProject.Controls.RoundPanel();
            this.roundButton1 = new WinFormSharedProject.Controls.RoundButton();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LogoPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.roundPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.SubTitlelabel);
            this.panel1.Controls.Add(this.Titlelabel);
            this.panel1.Controls.Add(this.LogoPictureBox);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(943, 76);
            this.panel1.TabIndex = 0;
            // 
            // SubTitlelabel
            // 
            this.SubTitlelabel.AutoSize = true;
            this.SubTitlelabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(254)));
            this.SubTitlelabel.Location = new System.Drawing.Point(464, 39);
            this.SubTitlelabel.Name = "SubTitlelabel";
            this.SubTitlelabel.Size = new System.Drawing.Size(66, 24);
            this.SubTitlelabel.TabIndex = 2;
            this.SubTitlelabel.Text = "label1";
            // 
            // Titlelabel
            // 
            this.Titlelabel.AutoSize = true;
            this.Titlelabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(254)));
            this.Titlelabel.Location = new System.Drawing.Point(452, 8);
            this.Titlelabel.Name = "Titlelabel";
            this.Titlelabel.Size = new System.Drawing.Size(92, 31);
            this.Titlelabel.TabIndex = 1;
            this.Titlelabel.Text = "label1";
            // 
            // LogoPictureBox
            // 
            this.LogoPictureBox.Image = global::WinFormAppDesigner.Properties.Resources._007_mobile_data;
            this.LogoPictureBox.Location = new System.Drawing.Point(3, 3);
            this.LogoPictureBox.Name = "LogoPictureBox";
            this.LogoPictureBox.Size = new System.Drawing.Size(83, 70);
            this.LogoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.LogoPictureBox.TabIndex = 0;
            this.LogoPictureBox.TabStop = false;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 76);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.NavigationTreeView);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.roundPanel1);
            this.splitContainer1.Size = new System.Drawing.Size(943, 543);
            this.splitContainer1.SplitterDistance = 219;
            this.splitContainer1.TabIndex = 1;
            // 
            // NavigationTreeView
            // 
            this.NavigationTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.NavigationTreeView.ImageIndex = 0;
            this.NavigationTreeView.ImageList = this.imageList1;
            this.NavigationTreeView.Location = new System.Drawing.Point(0, 0);
            this.NavigationTreeView.Name = "NavigationTreeView";
            this.NavigationTreeView.SelectedImageIndex = 0;
            this.NavigationTreeView.Size = new System.Drawing.Size(219, 543);
            this.NavigationTreeView.TabIndex = 0;
            // 
            // imageList1
            // 
            this.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.imageList1.ImageSize = new System.Drawing.Size(16, 16);
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // roundPanel1
            // 
            this.roundPanel1.ColorBottom = System.Drawing.Color.Azure;
            this.roundPanel1.ColorTop = System.Drawing.Color.RoyalBlue;
            this.roundPanel1.Controls.Add(this.roundButton1);
            this.roundPanel1.CornerRadius = 30;
            this.roundPanel1.Location = new System.Drawing.Point(95, 67);
            this.roundPanel1.Name = "roundPanel1";
            this.roundPanel1.Size = new System.Drawing.Size(564, 416);
            this.roundPanel1.TabIndex = 0;
            // 
            // roundButton1
            // 
            this.roundButton1.CornerRadius = 30;
            this.roundButton1.Location = new System.Drawing.Point(108, 110);
            this.roundButton1.Name = "roundButton1";
            this.roundButton1.RoundingStyle = WinFormSharedProject.Controls.RoundingStyle.Circle;
            this.roundButton1.Size = new System.Drawing.Size(104, 85);
            this.roundButton1.TabIndex = 0;
            this.roundButton1.Text = "roundButton1";
            this.roundButton1.UseVisualStyleBackColor = true;
            // 
            // uc_MainScreen
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.panel1);
            this.Name = "uc_MainScreen";
            this.Size = new System.Drawing.Size(943, 619);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LogoPictureBox)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.roundPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.PictureBox LogoPictureBox;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView NavigationTreeView;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.Label SubTitlelabel;
        private System.Windows.Forms.Label Titlelabel;
        private WinFormSharedProject.Controls.RoundPanel roundPanel1;
        private WinFormSharedProject.Controls.RoundButton roundButton1;
    }
}
