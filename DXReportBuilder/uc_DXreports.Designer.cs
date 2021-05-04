
namespace DXReportBuilder
{
    partial class uc_DXreports
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
            System.Windows.Forms.Label idLabel;
            System.Windows.Forms.Label reportDefinitionLabel;
            System.Windows.Forms.Label reportEngineLabel;
            System.Windows.Forms.Label reportNameLabel;
            System.Windows.Forms.Label label1;
            this.reportslistBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.idLabel1 = new System.Windows.Forms.Label();
            this.reportDefinitionLabel1 = new System.Windows.Forms.Label();
            this.reportEngineComboBox = new System.Windows.Forms.ComboBox();
            this.reportNameTextBox = new System.Windows.Forms.TextBox();
            this.Savebutton = new System.Windows.Forms.Button();
            idLabel = new System.Windows.Forms.Label();
            reportDefinitionLabel = new System.Windows.Forms.Label();
            reportEngineLabel = new System.Windows.Forms.Label();
            reportNameLabel = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.reportslistBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // idLabel
            // 
            idLabel.AutoSize = true;
            idLabel.Location = new System.Drawing.Point(105, 55);
            idLabel.Name = "idLabel";
            idLabel.Size = new System.Drawing.Size(18, 13);
            idLabel.TabIndex = 1;
            idLabel.Text = "id:";
            // 
            // reportDefinitionLabel
            // 
            reportDefinitionLabel.AutoSize = true;
            reportDefinitionLabel.Location = new System.Drawing.Point(34, 88);
            reportDefinitionLabel.Name = "reportDefinitionLabel";
            reportDefinitionLabel.Size = new System.Drawing.Size(89, 13);
            reportDefinitionLabel.TabIndex = 3;
            reportDefinitionLabel.Text = "Report Definition:";
            // 
            // reportEngineLabel
            // 
            reportEngineLabel.AutoSize = true;
            reportEngineLabel.Location = new System.Drawing.Point(45, 117);
            reportEngineLabel.Name = "reportEngineLabel";
            reportEngineLabel.Size = new System.Drawing.Size(78, 13);
            reportEngineLabel.TabIndex = 5;
            reportEngineLabel.Text = "Report Engine:";
            // 
            // reportNameLabel
            // 
            reportNameLabel.AutoSize = true;
            reportNameLabel.Location = new System.Drawing.Point(50, 144);
            reportNameLabel.Name = "reportNameLabel";
            reportNameLabel.Size = new System.Drawing.Size(73, 13);
            reportNameLabel.TabIndex = 7;
            reportNameLabel.Text = "Report Name:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Impact", 14F);
            label1.Location = new System.Drawing.Point(153, 15);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(63, 23);
            label1.TabIndex = 20;
            label1.Text = "Report";
            // 
            // reportslistBindingSource
            // 
            this.reportslistBindingSource.DataSource = typeof(TheTechIdea.DataManagment_Engine.Report.ReportsList);
            // 
            // idLabel1
            // 
            this.idLabel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.idLabel1.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.reportslistBindingSource, "id", true));
            this.idLabel1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.idLabel1.Location = new System.Drawing.Point(129, 54);
            this.idLabel1.Name = "idLabel1";
            this.idLabel1.Size = new System.Drawing.Size(121, 23);
            this.idLabel1.TabIndex = 2;
            // 
            // reportDefinitionLabel1
            // 
            this.reportDefinitionLabel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.reportDefinitionLabel1.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.reportslistBindingSource, "ReportDefinition", true));
            this.reportDefinitionLabel1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.reportDefinitionLabel1.Location = new System.Drawing.Point(129, 87);
            this.reportDefinitionLabel1.Name = "reportDefinitionLabel1";
            this.reportDefinitionLabel1.Size = new System.Drawing.Size(193, 23);
            this.reportDefinitionLabel1.TabIndex = 4;
            // 
            // reportEngineComboBox
            // 
            this.reportEngineComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.reportslistBindingSource, "ReportEngine", true));
            this.reportEngineComboBox.FormattingEnabled = true;
            this.reportEngineComboBox.Location = new System.Drawing.Point(129, 113);
            this.reportEngineComboBox.Name = "reportEngineComboBox";
            this.reportEngineComboBox.Size = new System.Drawing.Size(193, 21);
            this.reportEngineComboBox.TabIndex = 6;
            // 
            // reportNameTextBox
            // 
            this.reportNameTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.reportslistBindingSource, "ReportName", true));
            this.reportNameTextBox.Location = new System.Drawing.Point(129, 140);
            this.reportNameTextBox.Name = "reportNameTextBox";
            this.reportNameTextBox.Size = new System.Drawing.Size(193, 20);
            this.reportNameTextBox.TabIndex = 8;
            // 
            // Savebutton
            // 
            this.Savebutton.Location = new System.Drawing.Point(129, 185);
            this.Savebutton.Name = "Savebutton";
            this.Savebutton.Size = new System.Drawing.Size(121, 26);
            this.Savebutton.TabIndex = 21;
            this.Savebutton.Text = "Save";
            this.Savebutton.UseVisualStyleBackColor = true;
            // 
            // uc_DXreports
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Savebutton);
            this.Controls.Add(label1);
            this.Controls.Add(idLabel);
            this.Controls.Add(this.idLabel1);
            this.Controls.Add(reportDefinitionLabel);
            this.Controls.Add(this.reportDefinitionLabel1);
            this.Controls.Add(reportEngineLabel);
            this.Controls.Add(this.reportEngineComboBox);
            this.Controls.Add(reportNameLabel);
            this.Controls.Add(this.reportNameTextBox);
            this.Name = "uc_DXreports";
            this.Size = new System.Drawing.Size(365, 242);
            ((System.ComponentModel.ISupportInitialize)(this.reportslistBindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.BindingSource reportslistBindingSource;
        private System.Windows.Forms.Label idLabel1;
        private System.Windows.Forms.Label reportDefinitionLabel1;
        private System.Windows.Forms.ComboBox reportEngineComboBox;
        private System.Windows.Forms.TextBox reportNameTextBox;
        private System.Windows.Forms.Button Savebutton;
    }
}
