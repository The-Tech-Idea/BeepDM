
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
            this.Configbutton = new System.Windows.Forms.Button();
            this.DataConnectionbutton = new System.Windows.Forms.Button();
            this.Checkbutton = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // treeView1
            // 
            this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView1.FullRowSelect = true;
            this.treeView1.Location = new System.Drawing.Point(29, 0);
            this.treeView1.Margin = new System.Windows.Forms.Padding(2);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(391, 580);
            this.treeView1.TabIndex = 0;
            // 
            // Configbutton
            // 
            this.Configbutton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Configbutton.BackColor = System.Drawing.Color.Transparent;
            this.Configbutton.FlatAppearance.BorderSize = 0;
            this.Configbutton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Configbutton.Image = global::TheTechIdea.Properties.Resources.icons8_automatic_16;
            this.Configbutton.Location = new System.Drawing.Point(3, 552);
            this.Configbutton.Name = "Configbutton";
            this.Configbutton.Size = new System.Drawing.Size(20, 23);
            this.Configbutton.TabIndex = 3;
            this.Configbutton.UseCompatibleTextRendering = true;
            this.Configbutton.UseVisualStyleBackColor = false;
            // 
            // DataConnectionbutton
            // 
            this.DataConnectionbutton.BackColor = System.Drawing.Color.Transparent;
            this.DataConnectionbutton.FlatAppearance.BorderSize = 0;
            this.DataConnectionbutton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.DataConnectionbutton.Image = global::TheTechIdea.Properties.Resources.connections_16;
            this.DataConnectionbutton.Location = new System.Drawing.Point(3, 30);
            this.DataConnectionbutton.Name = "DataConnectionbutton";
            this.DataConnectionbutton.Size = new System.Drawing.Size(20, 23);
            this.DataConnectionbutton.TabIndex = 2;
            this.DataConnectionbutton.UseCompatibleTextRendering = true;
            this.DataConnectionbutton.UseVisualStyleBackColor = false;
            // 
            // Checkbutton
            // 
            this.Checkbutton.BackColor = System.Drawing.Color.Transparent;
            this.Checkbutton.FlatAppearance.BorderSize = 0;
            this.Checkbutton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Checkbutton.Image = global::TheTechIdea.Properties.Resources.checked_16;
            this.Checkbutton.Location = new System.Drawing.Point(3, 3);
            this.Checkbutton.Name = "Checkbutton";
            this.Checkbutton.Size = new System.Drawing.Size(20, 23);
            this.Checkbutton.TabIndex = 1;
            this.Checkbutton.UseCompatibleTextRendering = true;
            this.Checkbutton.UseVisualStyleBackColor = false;
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.DataConnectionbutton);
            this.panel1.Controls.Add(this.Configbutton);
            this.panel1.Controls.Add(this.Checkbutton);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(29, 580);
            this.panel1.TabIndex = 4;
            // 
            // uc_DynamicTree
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.panel1);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "uc_DynamicTree";
            this.Size = new System.Drawing.Size(420, 580);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Button Checkbutton;
        private System.Windows.Forms.Button DataConnectionbutton;
        private System.Windows.Forms.Button Configbutton;
        private System.Windows.Forms.Panel panel1;
    }
}
