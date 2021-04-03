using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace SimpleODM
{
    public partial class frm_Main 
    {


        // Form overrides dispose to clean up the component list.
        [DebuggerNonUserCode()]
        protected override void Dispose(bool disposing)
        {
            if (disposing && components is object)
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        // Required by the Windows Form Designer
        private System.ComponentModel.IContainer components;

        // NOTE: The following procedure is required by the Windows Form Designer
        // It can be modified using the Windows Form Designer.  
        // Do not modify it using the code editor.
        [DebuggerStepThrough()]
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(frm_Main));
            PanelControl1 = new DevExpress.XtraEditors.PanelControl();
            LoginSimpleButton = new DevExpress.XtraEditors.SimpleButton();
            LabelControl2 = new DevExpress.XtraEditors.LabelControl();
            LabelControl1 = new DevExpress.XtraEditors.LabelControl();
            AboutSimpleButton = new DevExpress.XtraEditors.SimpleButton();
            HelpSimpleButton = new DevExpress.XtraEditors.SimpleButton();
            MaxSimpleButton = new DevExpress.XtraEditors.SimpleButton();
            MinSimpleButton = new DevExpress.XtraEditors.SimpleButton();
            CloseSimpleButton = new DevExpress.XtraEditors.SimpleButton();
            PanelControl2 = new DevExpress.XtraEditors.PanelControl();
            Uc_tileMenuUI1 = new SimpleODM.SharedLib.uc_tileMenuUI();
            ((System.ComponentModel.ISupportInitialize)PanelControl1).BeginInit();
            PanelControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)PanelControl2).BeginInit();
            PanelControl2.SuspendLayout();
            this.SuspendLayout();
            // 
            // PanelControl1
            // 
            PanelControl1.Controls.Add(LoginSimpleButton);
            PanelControl1.Controls.Add(LabelControl2);
            PanelControl1.Controls.Add(LabelControl1);
            PanelControl1.Controls.Add(AboutSimpleButton);
            PanelControl1.Controls.Add(HelpSimpleButton);
            PanelControl1.Controls.Add(MaxSimpleButton);
            PanelControl1.Controls.Add(MinSimpleButton);
            PanelControl1.Controls.Add(CloseSimpleButton);
            PanelControl1.Dock = System.Windows.Forms.DockStyle.Top;
            PanelControl1.Location = new System.Drawing.Point(0, 0);
            PanelControl1.Name = "PanelControl1";
            PanelControl1.Size = new System.Drawing.Size(1200, 36);
            PanelControl1.TabIndex = 0;
            // 
            // LoginSimpleButton
            // 
            LoginSimpleButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            LoginSimpleButton.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
            LoginSimpleButton.Image = (System.Drawing.Image)resources.GetObject("LoginSimpleButton.Image");
            LoginSimpleButton.Location = new System.Drawing.Point(1008, 5);
            LoginSimpleButton.Name = "LoginSimpleButton";
            LoginSimpleButton.Size = new System.Drawing.Size(25, 26);
            LoginSimpleButton.TabIndex = 9;
            LoginSimpleButton.ToolTip = "Go back to Login";
            // 
            // LabelControl2
            // 
            LabelControl2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;

            LabelControl2.Appearance.Font = new System.Drawing.Font("Leelawadee UI", 10.0f, System.Drawing.FontStyle.Bold);
            LabelControl2.Appearance.ForeColor = System.Drawing.Color.Green;
            LabelControl2.Appearance.Options.UseFont = true;
            LabelControl2.Appearance.Options.UseForeColor = true;
            LabelControl2.Appearance.Options.UseTextOptions = true;
            LabelControl2.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            LabelControl2.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            LabelControl2.Location = new System.Drawing.Point(543, 8);
            LabelControl2.Name = "LabelControl2";
            LabelControl2.Size = new System.Drawing.Size(168, 28);
            LabelControl2.TabIndex = 8;
            LabelControl2.Text = "Enterprise Edition";
            // 
            // LabelControl1
            // 
            LabelControl1.Appearance.Font = new System.Drawing.Font("Leelawadee UI", 10.0f, System.Drawing.FontStyle.Bold);
            LabelControl1.Appearance.Options.UseFont = true;
            LabelControl1.Location = new System.Drawing.Point(46, 8);
            LabelControl1.Name = "LabelControl1";
            LabelControl1.Size = new System.Drawing.Size(130, 28);
            LabelControl1.TabIndex = 7;
            LabelControl1.Text = "The Tech Idea";
            // 
            // AboutSimpleButton
            // 
            AboutSimpleButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            AboutSimpleButton.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
            AboutSimpleButton.Image = (System.Drawing.Image)resources.GetObject("AboutSimpleButton.Image");
            AboutSimpleButton.Location = new System.Drawing.Point(1039, 6);
            AboutSimpleButton.Name = "AboutSimpleButton";
            AboutSimpleButton.Size = new System.Drawing.Size(26, 23);
            AboutSimpleButton.TabIndex = 4;
            AboutSimpleButton.ToolTip = "About";
            // 
            // HelpSimpleButton
            // 
            HelpSimpleButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            HelpSimpleButton.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
            HelpSimpleButton.Image = (System.Drawing.Image)resources.GetObject("HelpSimpleButton.Image");
            HelpSimpleButton.Location = new System.Drawing.Point(1071, 6);
            HelpSimpleButton.Name = "HelpSimpleButton";
            HelpSimpleButton.Size = new System.Drawing.Size(26, 23);
            HelpSimpleButton.TabIndex = 3;
            HelpSimpleButton.ToolTip = "Help";
            // 
            // MaxSimpleButton
            // 
            MaxSimpleButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            MaxSimpleButton.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
            MaxSimpleButton.Image = (System.Drawing.Image)resources.GetObject("MaxSimpleButton.Image");
            MaxSimpleButton.Location = new System.Drawing.Point(1103, 6);
            MaxSimpleButton.Name = "MaxSimpleButton";
            MaxSimpleButton.Size = new System.Drawing.Size(26, 23);
            MaxSimpleButton.TabIndex = 2;
            MaxSimpleButton.ToolTip = "Maximize";
            // 
            // MinSimpleButton
            // 
            MinSimpleButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            MinSimpleButton.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
            MinSimpleButton.Image = (System.Drawing.Image)resources.GetObject("MinSimpleButton.Image");
            MinSimpleButton.Location = new System.Drawing.Point(1135, 6);
            MinSimpleButton.Name = "MinSimpleButton";
            MinSimpleButton.Size = new System.Drawing.Size(26, 23);
            MinSimpleButton.TabIndex = 1;
            MinSimpleButton.ToolTip = "Minimize";
            // 
            // CloseSimpleButton
            // 
            CloseSimpleButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            CloseSimpleButton.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
            CloseSimpleButton.Image = (System.Drawing.Image)resources.GetObject("CloseSimpleButton.Image");
            CloseSimpleButton.Location = new System.Drawing.Point(1167, 6);
            CloseSimpleButton.Name = "CloseSimpleButton";
            CloseSimpleButton.Size = new System.Drawing.Size(26, 23);
            CloseSimpleButton.TabIndex = 0;
            CloseSimpleButton.ToolTip = "Close Application";
            // 
            // PanelControl2
            // 
            PanelControl2.Controls.Add(Uc_tileMenuUI1);
            PanelControl2.Location = new System.Drawing.Point(0, 36);
            PanelControl2.Name = "PanelControl2";
            PanelControl2.Size = new System.Drawing.Size(1200, 764);
            PanelControl2.TabIndex = 1;
            // 
            // Uc_tileMenuUI1
            // 
            Uc_tileMenuUI1.AutoLoginON = false;
            Uc_tileMenuUI1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            Uc_tileMenuUI1.Dock = System.Windows.Forms.DockStyle.Fill;
            Uc_tileMenuUI1.Location = new System.Drawing.Point(3, 3);
            Uc_tileMenuUI1.LookAndFeel.SkinName = "Metropolis";
            Uc_tileMenuUI1.LookAndFeel.UseWindowsXPTheme = true;
            Uc_tileMenuUI1.Margin = new System.Windows.Forms.Padding(4);
            Uc_tileMenuUI1.Name = "Uc_tileMenuUI1";
            Uc_tileMenuUI1.Size = new System.Drawing.Size(1194, 758);
            Uc_tileMenuUI1.TabIndex = 0;
            // 
            // frm_Main
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.ClientSize = new System.Drawing.Size(1200, 800);
            this.Controls.Add(PanelControl2);
            this.Controls.Add(PanelControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            this.Name = "frm_Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "frm_Main";
            ((System.ComponentModel.ISupportInitialize)PanelControl1).EndInit();
            PanelControl1.ResumeLayout(false);
            PanelControl1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)PanelControl2).EndInit();
            PanelControl2.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        public DevExpress.XtraEditors.PanelControl PanelControl1;
        public DevExpress.XtraEditors.PanelControl PanelControl2;
        public SimpleODM.SharedLib.uc_tileMenuUI Uc_tileMenuUI1;
        public DevExpress.XtraEditors.SimpleButton AboutSimpleButton;
        public DevExpress.XtraEditors.SimpleButton HelpSimpleButton;
        public DevExpress.XtraEditors.SimpleButton MaxSimpleButton;
        public DevExpress.XtraEditors.SimpleButton MinSimpleButton;
        public DevExpress.XtraEditors.SimpleButton CloseSimpleButton;
        public DevExpress.XtraEditors.LabelControl LabelControl2;
        public DevExpress.XtraEditors.LabelControl LabelControl1;
        public DevExpress.XtraEditors.SimpleButton LoginSimpleButton;
    }
}
