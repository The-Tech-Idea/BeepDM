
namespace TheTechIdea.Beep.AppBuilder.UserControls
{
    partial class uc_updateentity
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
            this.EntitybindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.subtitlelabel = new System.Windows.Forms.Label();
            this.EntityNamelabel = new System.Windows.Forms.Label();
            this.SaveEntitybutton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.EntitybindingSource)).BeginInit();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 92);
            this.panel1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(968, 1034);
            this.panel1.TabIndex = 0;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.subtitlelabel);
            this.panel2.Controls.Add(this.EntityNamelabel);
            this.panel2.Controls.Add(this.SaveEntitybutton);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(968, 92);
            this.panel2.TabIndex = 0;
            // 
            // subtitlelabel
            // 
            this.subtitlelabel.AutoSize = true;
            this.subtitlelabel.ForeColor = System.Drawing.Color.SteelBlue;
            this.subtitlelabel.Location = new System.Drawing.Point(40, 52);
            this.subtitlelabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.subtitlelabel.Name = "subtitlelabel";
            this.subtitlelabel.Size = new System.Drawing.Size(99, 20);
            this.subtitlelabel.TabIndex = 8;
            this.subtitlelabel.Text = "Data Source";
            // 
            // EntityNamelabel
            // 
            this.EntityNamelabel.AutoSize = true;
            this.EntityNamelabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.EntityNamelabel.ForeColor = System.Drawing.Color.SteelBlue;
            this.EntityNamelabel.Location = new System.Drawing.Point(39, 9);
            this.EntityNamelabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.EntityNamelabel.Name = "EntityNamelabel";
            this.EntityNamelabel.Size = new System.Drawing.Size(153, 29);
            this.EntityNamelabel.TabIndex = 6;
            this.EntityNamelabel.Text = "Entity Name";
            // 
            // SaveEntitybutton
            // 
            this.SaveEntitybutton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SaveEntitybutton.Location = new System.Drawing.Point(837, 52);
            this.SaveEntitybutton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.SaveEntitybutton.Name = "SaveEntitybutton";
            this.SaveEntitybutton.Size = new System.Drawing.Size(112, 35);
            this.SaveEntitybutton.TabIndex = 7;
            this.SaveEntitybutton.Text = "Save";
            this.SaveEntitybutton.UseVisualStyleBackColor = true;
            // 
            // uc_updateentity
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panel2);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "uc_updateentity";
            this.Size = new System.Drawing.Size(968, 1126);
            ((System.ComponentModel.ISupportInitialize)(this.EntitybindingSource)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.BindingSource EntitybindingSource;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label subtitlelabel;
        private System.Windows.Forms.Label EntityNamelabel;
        private System.Windows.Forms.Button SaveEntitybutton;
    }
}
