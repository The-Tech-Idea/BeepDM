
namespace TheTechIdea.Winforms.VIS
{
    partial class Frm_Waiting
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.Captionlabel = new System.Windows.Forms.TextBox();
            this.DetailsTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(12, 74);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(269, 23);
            this.progressBar1.TabIndex = 0;
            // 
            // Captionlabel
            // 
            this.Captionlabel.BackColor = System.Drawing.Color.White;
            this.Captionlabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Captionlabel.Location = new System.Drawing.Point(12, 12);
            this.Captionlabel.Multiline = true;
            this.Captionlabel.Name = "Captionlabel";
            this.Captionlabel.ReadOnly = true;
            this.Captionlabel.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.Captionlabel.Size = new System.Drawing.Size(269, 56);
            this.Captionlabel.TabIndex = 3;
            this.Captionlabel.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // DetailsTextBox
            // 
            this.DetailsTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DetailsTextBox.BackColor = System.Drawing.Color.White;
            this.DetailsTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.DetailsTextBox.Location = new System.Drawing.Point(12, 104);
            this.DetailsTextBox.Multiline = true;
            this.DetailsTextBox.Name = "DetailsTextBox";
            this.DetailsTextBox.ReadOnly = true;
            this.DetailsTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.DetailsTextBox.Size = new System.Drawing.Size(271, 379);
            this.DetailsTextBox.TabIndex = 4;
            // 
            // Frm_Waiting
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Lavender;
            this.ClientSize = new System.Drawing.Size(295, 495);
            this.ControlBox = false;
            this.Controls.Add(this.DetailsTextBox);
            this.Controls.Add(this.Captionlabel);
            this.Controls.Add(this.progressBar1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "Frm_Waiting";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "                                Please Wait";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.TextBox Captionlabel;
        private System.Windows.Forms.TextBox DetailsTextBox;
    }
}