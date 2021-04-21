
namespace TheTechIdea.Hidden
{
    partial class uc_DynamicTree
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
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.Checkbutton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // treeView1
            // 
            this.treeView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeView1.FullRowSelect = true;
            this.treeView1.Location = new System.Drawing.Point(21, 0);
            this.treeView1.Margin = new System.Windows.Forms.Padding(2);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(399, 580);
            this.treeView1.TabIndex = 0;
            // 
            // Checkbutton
            // 
            this.Checkbutton.BackColor = System.Drawing.Color.Transparent;
            this.Checkbutton.FlatAppearance.BorderSize = 0;
            this.Checkbutton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Checkbutton.Image = global::TheTechIdea.Properties.Resources.checked_16;
            this.Checkbutton.Location = new System.Drawing.Point(0, 0);
            this.Checkbutton.Name = "Checkbutton";
            this.Checkbutton.Size = new System.Drawing.Size(20, 23);
            this.Checkbutton.TabIndex = 1;
            this.Checkbutton.UseCompatibleTextRendering = true;
            this.Checkbutton.UseVisualStyleBackColor = false;
            // 
            // uc_DynamicTree
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Checkbutton);
            this.Controls.Add(this.treeView1);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "uc_DynamicTree";
            this.Size = new System.Drawing.Size(420, 580);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Button Checkbutton;
    }
}
