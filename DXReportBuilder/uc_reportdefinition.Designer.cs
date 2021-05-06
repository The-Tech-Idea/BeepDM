
namespace DXReportBuilder
{
    partial class uc_reportdefinition
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
            System.Windows.Forms.Label packageNameLabel;
            System.Windows.Forms.Label label1;
            System.Windows.Forms.Label entityIDLabel;
            System.Windows.Forms.Label viewIDLabel;
            System.Windows.Forms.Label nameLabel;
            this.reportsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.blocksBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.reportWritersClassesBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.blockColumnsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.fontDialog1 = new System.Windows.Forms.FontDialog();
            this.RemoveBlockbutton = new System.Windows.Forms.Button();
            this.ReportOutPutTypecomboBox = new System.Windows.Forms.ComboBox();
            this.blockColumnsDataGridView = new System.Windows.Forms.DataGridView();
            this.Show = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DisplayName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.GridViewFontButton = new System.Windows.Forms.DataGridViewButtonColumn();
            this.GridViewForeColor = new System.Windows.Forms.DataGridViewButtonColumn();
            this.GridViewBackColor = new System.Windows.Forms.DataGridViewButtonColumn();
            this.packageNameComboBox = new System.Windows.Forms.ComboBox();
            this.RunReportbutton = new System.Windows.Forms.Button();
            this.Savebutton = new System.Windows.Forms.Button();
            this.entityIDComboBox = new System.Windows.Forms.ComboBox();
            this.viewIDComboBox = new System.Windows.Forms.ComboBox();
            this.AddBlockbutton = new System.Windows.Forms.Button();
            this.blocksDataGridView = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn14 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.nameTextBox = new System.Windows.Forms.TextBox();
            packageNameLabel = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            entityIDLabel = new System.Windows.Forms.Label();
            viewIDLabel = new System.Windows.Forms.Label();
            nameLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.reportsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.blocksBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.reportWritersClassesBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.blockColumnsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.blockColumnsDataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.blocksDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // packageNameLabel
            // 
            packageNameLabel.AutoSize = true;
            packageNameLabel.Location = new System.Drawing.Point(72, 739);
            packageNameLabel.Name = "packageNameLabel";
            packageNameLabel.Size = new System.Drawing.Size(92, 13);
            packageNameLabel.TabIndex = 53;
            packageNameLabel.Text = "Report Generator:";
            packageNameLabel.Visible = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Impact", 14F);
            label1.Location = new System.Drawing.Point(224, 9);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(144, 23);
            label1.TabIndex = 50;
            label1.Text = "Report Definition";
            // 
            // entityIDLabel
            // 
            entityIDLabel.AutoSize = true;
            entityIDLabel.Location = new System.Drawing.Point(266, 104);
            entityIDLabel.Name = "entityIDLabel";
            entityIDLabel.Size = new System.Drawing.Size(50, 13);
            entityIDLabel.TabIndex = 48;
            entityIDLabel.Text = "Entity ID:";
            // 
            // viewIDLabel
            // 
            viewIDLabel.AutoSize = true;
            viewIDLabel.Location = new System.Drawing.Point(56, 104);
            viewIDLabel.Name = "viewIDLabel";
            viewIDLabel.Size = new System.Drawing.Size(47, 13);
            viewIDLabel.TabIndex = 46;
            viewIDLabel.Text = "View ID:";
            // 
            // nameLabel
            // 
            nameLabel.AutoSize = true;
            nameLabel.Location = new System.Drawing.Point(38, 49);
            nameLabel.Name = "nameLabel";
            nameLabel.Size = new System.Drawing.Size(38, 13);
            nameLabel.TabIndex = 36;
            nameLabel.Text = "Name:";
            // 
            // reportsBindingSource
            // 
            this.reportsBindingSource.DataSource = typeof(TheTechIdea.DataManagment_Engine.Report.ReportTemplate);
            // 
            // blocksBindingSource
            // 
            this.blocksBindingSource.AllowNew = true;
            this.blocksBindingSource.DataMember = "Blocks";
            this.blocksBindingSource.DataSource = this.reportsBindingSource;
            // 
            // reportWritersClassesBindingSource
            // 
            this.reportWritersClassesBindingSource.DataSource = typeof(TheTechIdea.Util.AssemblyClassDefinition);
            // 
            // blockColumnsBindingSource
            // 
            this.blockColumnsBindingSource.DataMember = "BlockColumns";
            this.blockColumnsBindingSource.DataSource = this.blocksBindingSource;
            // 
            // colorDialog1
            // 
            this.colorDialog1.FullOpen = true;
            // 
            // RemoveBlockbutton
            // 
            this.RemoveBlockbutton.Location = new System.Drawing.Point(459, 504);
            this.RemoveBlockbutton.Name = "RemoveBlockbutton";
            this.RemoveBlockbutton.Size = new System.Drawing.Size(75, 23);
            this.RemoveBlockbutton.TabIndex = 64;
            this.RemoveBlockbutton.Text = "Remove";
            this.RemoveBlockbutton.UseVisualStyleBackColor = true;
            // 
            // ReportOutPutTypecomboBox
            // 
            this.ReportOutPutTypecomboBox.FormattingEnabled = true;
            this.ReportOutPutTypecomboBox.Location = new System.Drawing.Point(374, 736);
            this.ReportOutPutTypecomboBox.Name = "ReportOutPutTypecomboBox";
            this.ReportOutPutTypecomboBox.Size = new System.Drawing.Size(94, 21);
            this.ReportOutPutTypecomboBox.TabIndex = 63;
            this.ReportOutPutTypecomboBox.Visible = false;
            // 
            // blockColumnsDataGridView
            // 
            this.blockColumnsDataGridView.AutoGenerateColumns = false;
            this.blockColumnsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.blockColumnsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Show,
            this.dataGridViewTextBoxColumn4,
            this.DisplayName,
            this.dataGridViewTextBoxColumn3,
            this.GridViewFontButton,
            this.GridViewForeColor,
            this.GridViewBackColor});
            this.blockColumnsDataGridView.DataSource = this.blockColumnsBindingSource;
            this.blockColumnsDataGridView.Location = new System.Drawing.Point(604, 275);
            this.blockColumnsDataGridView.Name = "blockColumnsDataGridView";
            this.blockColumnsDataGridView.Size = new System.Drawing.Size(477, 183);
            this.blockColumnsDataGridView.TabIndex = 55;
            this.blockColumnsDataGridView.Visible = false;
            // 
            // Show
            // 
            this.Show.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.Show.DataPropertyName = "Show";
            this.Show.HeaderText = "Show";
            this.Show.Name = "Show";
            this.Show.Width = 40;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn4.DataPropertyName = "ColumnSeq";
            this.dataGridViewTextBoxColumn4.HeaderText = "Seq";
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.Width = 51;
            // 
            // DisplayName
            // 
            this.DisplayName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.DisplayName.DataPropertyName = "DisplayName";
            this.DisplayName.HeaderText = "DisplayName";
            this.DisplayName.Name = "DisplayName";
            this.DisplayName.Width = 94;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn3.DataPropertyName = "ColumnName";
            this.dataGridViewTextBoxColumn3.HeaderText = "ColumnName";
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.Width = 95;
            // 
            // GridViewFontButton
            // 
            this.GridViewFontButton.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.GridViewFontButton.HeaderText = "Font";
            this.GridViewFontButton.Name = "GridViewFontButton";
            this.GridViewFontButton.Text = "Font";
            this.GridViewFontButton.UseColumnTextForButtonValue = true;
            this.GridViewFontButton.Width = 34;
            // 
            // GridViewForeColor
            // 
            this.GridViewForeColor.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.GridViewForeColor.HeaderText = "ForeColor";
            this.GridViewForeColor.Name = "GridViewForeColor";
            this.GridViewForeColor.Width = 58;
            // 
            // GridViewBackColor
            // 
            this.GridViewBackColor.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.GridViewBackColor.HeaderText = "BackColor";
            this.GridViewBackColor.Name = "GridViewBackColor";
            this.GridViewBackColor.Width = 62;
            // 
            // packageNameComboBox
            // 
            this.packageNameComboBox.DataSource = this.reportWritersClassesBindingSource;
            this.packageNameComboBox.DisplayMember = "className";
            this.packageNameComboBox.FormattingEnabled = true;
            this.packageNameComboBox.Location = new System.Drawing.Point(170, 736);
            this.packageNameComboBox.Name = "packageNameComboBox";
            this.packageNameComboBox.Size = new System.Drawing.Size(198, 21);
            this.packageNameComboBox.TabIndex = 54;
            this.packageNameComboBox.ValueMember = "PackageName";
            this.packageNameComboBox.Visible = false;
            // 
            // RunReportbutton
            // 
            this.RunReportbutton.Location = new System.Drawing.Point(474, 734);
            this.RunReportbutton.Name = "RunReportbutton";
            this.RunReportbutton.Size = new System.Drawing.Size(75, 23);
            this.RunReportbutton.TabIndex = 52;
            this.RunReportbutton.Text = "Run";
            this.RunReportbutton.UseVisualStyleBackColor = true;
            this.RunReportbutton.Visible = false;
            // 
            // Savebutton
            // 
            this.Savebutton.Location = new System.Drawing.Point(206, 532);
            this.Savebutton.Name = "Savebutton";
            this.Savebutton.Size = new System.Drawing.Size(162, 23);
            this.Savebutton.TabIndex = 51;
            this.Savebutton.Text = "Save";
            this.Savebutton.UseVisualStyleBackColor = true;
            // 
            // entityIDComboBox
            // 
            this.entityIDComboBox.FormattingEnabled = true;
            this.entityIDComboBox.Location = new System.Drawing.Point(322, 101);
            this.entityIDComboBox.Name = "entityIDComboBox";
            this.entityIDComboBox.Size = new System.Drawing.Size(121, 21);
            this.entityIDComboBox.TabIndex = 49;
            // 
            // viewIDComboBox
            // 
            this.viewIDComboBox.FormattingEnabled = true;
            this.viewIDComboBox.Location = new System.Drawing.Point(109, 101);
            this.viewIDComboBox.Name = "viewIDComboBox";
            this.viewIDComboBox.Size = new System.Drawing.Size(121, 21);
            this.viewIDComboBox.TabIndex = 47;
            // 
            // AddBlockbutton
            // 
            this.AddBlockbutton.Location = new System.Drawing.Point(459, 99);
            this.AddBlockbutton.Name = "AddBlockbutton";
            this.AddBlockbutton.Size = new System.Drawing.Size(75, 23);
            this.AddBlockbutton.TabIndex = 45;
            this.AddBlockbutton.Text = "Add Block";
            this.AddBlockbutton.UseVisualStyleBackColor = true;
            // 
            // blocksDataGridView
            // 
            this.blocksDataGridView.AutoGenerateColumns = false;
            this.blocksDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.blocksDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn1,
            this.dataGridViewTextBoxColumn2,
            this.dataGridViewTextBoxColumn14});
            this.blocksDataGridView.DataSource = this.blocksBindingSource;
            this.blocksDataGridView.Location = new System.Drawing.Point(59, 128);
            this.blocksDataGridView.Name = "blocksDataGridView";
            this.blocksDataGridView.ReadOnly = true;
            this.blocksDataGridView.Size = new System.Drawing.Size(475, 370);
            this.blocksDataGridView.TabIndex = 44;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.dataGridViewTextBoxColumn1.DataPropertyName = "EntityID";
            this.dataGridViewTextBoxColumn1.HeaderText = "EntityID";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Width = 69;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.dataGridViewTextBoxColumn2.DataPropertyName = "ViewID";
            this.dataGridViewTextBoxColumn2.HeaderText = "ViewID";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn14
            // 
            this.dataGridViewTextBoxColumn14.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn14.DataPropertyName = "ViewType";
            this.dataGridViewTextBoxColumn14.HeaderText = "ViewType";
            this.dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
            this.dataGridViewTextBoxColumn14.ReadOnly = true;
            this.dataGridViewTextBoxColumn14.Width = 79;
            // 
            // nameTextBox
            // 
            this.nameTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.reportsBindingSource, "Name", true));
            this.nameTextBox.Location = new System.Drawing.Point(82, 46);
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.Size = new System.Drawing.Size(452, 20);
            this.nameTextBox.TabIndex = 37;
            // 
            // uc_reportdefinition
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.RemoveBlockbutton);
            this.Controls.Add(this.ReportOutPutTypecomboBox);
            this.Controls.Add(this.blockColumnsDataGridView);
            this.Controls.Add(packageNameLabel);
            this.Controls.Add(this.packageNameComboBox);
            this.Controls.Add(this.RunReportbutton);
            this.Controls.Add(this.Savebutton);
            this.Controls.Add(label1);
            this.Controls.Add(entityIDLabel);
            this.Controls.Add(this.entityIDComboBox);
            this.Controls.Add(viewIDLabel);
            this.Controls.Add(this.viewIDComboBox);
            this.Controls.Add(this.AddBlockbutton);
            this.Controls.Add(this.blocksDataGridView);
            this.Controls.Add(nameLabel);
            this.Controls.Add(this.nameTextBox);
            this.Name = "uc_reportdefinition";
            this.Size = new System.Drawing.Size(594, 571);
            ((System.ComponentModel.ISupportInitialize)(this.reportsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.blocksBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.reportWritersClassesBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.blockColumnsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.blockColumnsDataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.blocksDataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.BindingSource reportsBindingSource;
        private System.Windows.Forms.BindingSource blocksBindingSource;
        private System.Windows.Forms.BindingSource reportWritersClassesBindingSource;
        private System.Windows.Forms.BindingSource blockColumnsBindingSource;
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.FontDialog fontDialog1;
        private System.Windows.Forms.Button RemoveBlockbutton;
        private System.Windows.Forms.ComboBox ReportOutPutTypecomboBox;
        private System.Windows.Forms.DataGridView blockColumnsDataGridView;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Show;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn DisplayName;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewButtonColumn GridViewFontButton;
        private System.Windows.Forms.DataGridViewButtonColumn GridViewForeColor;
        private System.Windows.Forms.DataGridViewButtonColumn GridViewBackColor;
        private System.Windows.Forms.ComboBox packageNameComboBox;
        private System.Windows.Forms.Button RunReportbutton;
        private System.Windows.Forms.Button Savebutton;
        private System.Windows.Forms.ComboBox entityIDComboBox;
        private System.Windows.Forms.ComboBox viewIDComboBox;
        private System.Windows.Forms.Button AddBlockbutton;
        private System.Windows.Forms.DataGridView blocksDataGridView;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn14;
        private System.Windows.Forms.TextBox nameTextBox;
    }
}
