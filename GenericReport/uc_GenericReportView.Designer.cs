
namespace TheTechIdea.DataManagment_Engine.Report
{
    partial class uc_GenericReportView
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
            this.printDocument1 = new System.Drawing.Printing.PrintDocument();
            this.printPreviewControl1 = new System.Windows.Forms.PrintPreviewControl();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.Printbutton = new System.Windows.Forms.Button();
            this.PageSetUpbutton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // printPreviewControl1
            // 
            this.printPreviewControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.printPreviewControl1.Location = new System.Drawing.Point(59, 3);
            this.printPreviewControl1.Name = "printPreviewControl1";
            this.printPreviewControl1.Size = new System.Drawing.Size(868, 644);
            this.printPreviewControl1.TabIndex = 0;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(930, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // Printbutton
            // 
            this.Printbutton.Image = global::TheTechIdea.DataManagment_Engine.Report.Properties.Resources.page_setup_32;
            this.Printbutton.Location = new System.Drawing.Point(3, 50);
            this.Printbutton.Name = "Printbutton";
            this.Printbutton.Size = new System.Drawing.Size(53, 41);
            this.Printbutton.TabIndex = 3;
            this.Printbutton.UseVisualStyleBackColor = true;
            // 
            // PageSetUpbutton
            // 
            this.PageSetUpbutton.Image = global::TheTechIdea.DataManagment_Engine.Report.Properties.Resources.settings_32;
            this.PageSetUpbutton.Location = new System.Drawing.Point(3, 3);
            this.PageSetUpbutton.Name = "PageSetUpbutton";
            this.PageSetUpbutton.Size = new System.Drawing.Size(53, 41);
            this.PageSetUpbutton.TabIndex = 2;
            this.PageSetUpbutton.UseVisualStyleBackColor = true;
            // 
            // uc_GenericReportView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Printbutton);
            this.Controls.Add(this.PageSetUpbutton);
            this.Controls.Add(this.printPreviewControl1);
            this.Controls.Add(this.menuStrip1);
            this.Name = "uc_GenericReportView";
            this.Size = new System.Drawing.Size(930, 647);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        public System.Windows.Forms.PrintPreviewControl printPreviewControl1;
        public System.Drawing.Printing.PrintDocument printDocument1;
        public System.Windows.Forms.MenuStrip menuStrip1;
        public System.Windows.Forms.Button PageSetUpbutton;
        public System.Windows.Forms.Button Printbutton;
    }
}
