
namespace DemoApp
{
    partial class Form1
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
            this.Datasourcesbutton = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // Datasourcesbutton
            // 
            this.Datasourcesbutton.Location = new System.Drawing.Point(29, 60);
            this.Datasourcesbutton.Name = "Datasourcesbutton";
            this.Datasourcesbutton.Size = new System.Drawing.Size(171, 23);
            this.Datasourcesbutton.TabIndex = 0;
            this.Datasourcesbutton.Text = "Data Sources";
            this.Datasourcesbutton.UseVisualStyleBackColor = true;
         
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(29, 89);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(171, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "Data Sources";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1392, 779);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.Datasourcesbutton);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button Datasourcesbutton;
        private System.Windows.Forms.Button button1;
    }
}

