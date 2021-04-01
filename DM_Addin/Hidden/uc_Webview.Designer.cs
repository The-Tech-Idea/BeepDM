
namespace TheTechIdea.Hidden
{
    partial class uc_Webview
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
            this.webView21 = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.Titlelabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // webView21
            // 
            this.webView21.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.webView21.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.webView21.CreationProperties = null;
            this.webView21.Location = new System.Drawing.Point(0, 36);
            this.webView21.Name = "webView21";
            this.webView21.Size = new System.Drawing.Size(1018, 602);
            this.webView21.TabIndex = 0;
            //this.webView21.Text = "webView21";
            this.webView21.ZoomFactor = 1D;
            // 
            // Titlelabel
            // 
            this.Titlelabel.AutoSize = true;
            this.Titlelabel.Location = new System.Drawing.Point(19, 10);
            this.Titlelabel.Name = "Titlelabel";
            this.Titlelabel.Size = new System.Drawing.Size(0, 13);
            this.Titlelabel.TabIndex = 1;
            // 
            // uc_Webview
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Titlelabel);
            this.Controls.Add(this.webView21);
            this.Name = "uc_Webview";
            this.Size = new System.Drawing.Size(1018, 638);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 webView21;
        private System.Windows.Forms.Label Titlelabel;
    }
}
