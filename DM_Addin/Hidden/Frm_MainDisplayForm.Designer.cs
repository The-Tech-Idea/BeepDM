namespace TheTechIdea.Hidden
{
    partial class Frm_MainDisplayForm
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
            this.DisplayControlsplitContainer1 = new System.Windows.Forms.SplitContainer();
            this.MenusplitContainer = new System.Windows.Forms.SplitContainer();
            this.uc_DynamicTree1 = new TheTechIdea.Hidden.uc_DynamicTree();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.uc_logpanel1 = new TheTechIdea.Hidden.uc_logpanel();
            ((System.ComponentModel.ISupportInitialize)(this.DisplayControlsplitContainer1)).BeginInit();
            this.DisplayControlsplitContainer1.Panel1.SuspendLayout();
            this.DisplayControlsplitContainer1.Panel2.SuspendLayout();
            this.DisplayControlsplitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MenusplitContainer)).BeginInit();
            this.MenusplitContainer.Panel1.SuspendLayout();
            this.MenusplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // DisplayControlsplitContainer1
            // 
            this.DisplayControlsplitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.DisplayControlsplitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DisplayControlsplitContainer1.Location = new System.Drawing.Point(0, 0);
            this.DisplayControlsplitContainer1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.DisplayControlsplitContainer1.Name = "DisplayControlsplitContainer1";
            // 
            // DisplayControlsplitContainer1.Panel1
            // 
            this.DisplayControlsplitContainer1.Panel1.Controls.Add(this.MenusplitContainer);
            // 
            // DisplayControlsplitContainer1.Panel2
            // 
            this.DisplayControlsplitContainer1.Panel2.Controls.Add(this.splitContainer1);
            this.DisplayControlsplitContainer1.Size = new System.Drawing.Size(2164, 1144);
            this.DisplayControlsplitContainer1.SplitterDistance = 458;
            this.DisplayControlsplitContainer1.SplitterWidth = 6;
            this.DisplayControlsplitContainer1.TabIndex = 0;
            // 
            // MenusplitContainer
            // 
            this.MenusplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MenusplitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.MenusplitContainer.Location = new System.Drawing.Point(0, 0);
            this.MenusplitContainer.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MenusplitContainer.Name = "MenusplitContainer";
            this.MenusplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // MenusplitContainer.Panel1
            // 
            this.MenusplitContainer.Panel1.Controls.Add(this.uc_DynamicTree1);
            this.MenusplitContainer.Panel2Collapsed = true;
            this.MenusplitContainer.Size = new System.Drawing.Size(456, 1142);
            this.MenusplitContainer.SplitterDistance = 1087;
            this.MenusplitContainer.SplitterWidth = 6;
            this.MenusplitContainer.TabIndex = 0;
            // 
            // uc_DynamicTree1
            // 
            this.uc_DynamicTree1.AddinName = "Dynamic Data View Tree";
            this.uc_DynamicTree1.Passedarg = null;
            this.uc_DynamicTree1.AutoScroll = true;
            this.uc_DynamicTree1.AutoScrollMargin = new System.Drawing.Size(5, 5);
            this.uc_DynamicTree1.AutoScrollMinSize = new System.Drawing.Size(5, 5);
            this.uc_DynamicTree1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.uc_DynamicTree1.DefaultCreate = false;
            this.uc_DynamicTree1.Description = "Dynamic Data View Tree";
            this.uc_DynamicTree1.DllName = null;
            this.uc_DynamicTree1.DllPath = null;
            this.uc_DynamicTree1.DMEEditor = null;
            this.uc_DynamicTree1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uc_DynamicTree1.Dset = null;
            this.uc_DynamicTree1.EntityName = null;
            this.uc_DynamicTree1.EntityStructure = null;
            this.uc_DynamicTree1.ErrorObject = null;
            this.uc_DynamicTree1.Location = new System.Drawing.Point(0, 0);
            this.uc_DynamicTree1.Logger = null;
            this.uc_DynamicTree1.Name = "uc_DynamicTree1";
            this.uc_DynamicTree1.NameSpace = null;
            this.uc_DynamicTree1.ObjectName = null;
            this.uc_DynamicTree1.ObjectType = "UserControl";
            this.uc_DynamicTree1.ParentName = null;
            this.uc_DynamicTree1.Size = new System.Drawing.Size(456, 1142);
            this.uc_DynamicTree1.TabIndex = 2;
            this.uc_DynamicTree1.Visutil = null;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.uc_logpanel1);
            this.splitContainer1.Size = new System.Drawing.Size(1698, 1142);
            this.splitContainer1.SplitterDistance = 982;
            this.splitContainer1.TabIndex = 1;
            // 
            // uc_logpanel1
            // 
            this.uc_logpanel1.AddinName = "Log Panel";
            this.uc_logpanel1.Passedarg = null;
            this.uc_logpanel1.DefaultCreate = false;
            this.uc_logpanel1.Description = "Log all Messeges";
            this.uc_logpanel1.DestConnection = null;
            this.uc_logpanel1.DllName = null;
            this.uc_logpanel1.DllPath = null;
            this.uc_logpanel1.DMEEditor = null;
            this.uc_logpanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uc_logpanel1.Dset = null;
            this.uc_logpanel1.EntityName = null;
            this.uc_logpanel1.EntityStructure = null;
            this.uc_logpanel1.ErrorObject = null;
            this.uc_logpanel1.Location = new System.Drawing.Point(0, 0);
            this.uc_logpanel1.Logger = null;
            this.uc_logpanel1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.uc_logpanel1.Name = "uc_logpanel1";
            this.uc_logpanel1.NameSpace = null;
            this.uc_logpanel1.ObjectName = null;
            this.uc_logpanel1.ObjectType = "UserControl";
            this.uc_logpanel1.ParentName = null;
            this.uc_logpanel1.Size = new System.Drawing.Size(1698, 156);
            this.uc_logpanel1.SourceConnection = null;
            this.uc_logpanel1.TabIndex = 0;
            // 
            // Frm_MainDisplayForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2164, 1144);
            this.Controls.Add(this.DisplayControlsplitContainer1);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "Frm_MainDisplayForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Frm_MainDisplayForm";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.DisplayControlsplitContainer1.Panel1.ResumeLayout(false);
            this.DisplayControlsplitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.DisplayControlsplitContainer1)).EndInit();
            this.DisplayControlsplitContainer1.ResumeLayout(false);
            this.MenusplitContainer.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.MenusplitContainer)).EndInit();
            this.MenusplitContainer.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer DisplayControlsplitContainer1;
        private System.Windows.Forms.SplitContainer MenusplitContainer;
        private Hidden.uc_logpanel uc_logpanel1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private uc_DynamicTree uc_DynamicTree1;
    }
}