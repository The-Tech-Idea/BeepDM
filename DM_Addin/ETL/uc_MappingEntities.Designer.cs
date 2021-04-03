namespace TheTechIdea.ETL
{
    partial class uc_MappingEntities
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
            System.Windows.Forms.Label entity1DataSourceLabel;
            System.Windows.Forms.Label entityName1Label;
            System.Windows.Forms.Label entity2DataSourceLabel;
            System.Windows.Forms.Label entityName2Label;
            System.Windows.Forms.Label mappingNameLabel;
            this.fldMappingBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.mappingsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.fldMappingDataGridView = new System.Windows.Forms.DataGridView();
            this.FieldName1Comobobox = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.FieldType1Comobobox = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FieldName2Comobobox = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.FieldType2Comobobox = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.entity1DataSourceComboBox = new System.Windows.Forms.ComboBox();
            this.entityName1ComboBox = new System.Windows.Forms.ComboBox();
            this.entity2DataSourceComboBox = new System.Windows.Forms.ComboBox();
            this.entityName2ComboBox = new System.Windows.Forms.ComboBox();
            this.mappingNameTextBox = new System.Windows.Forms.TextBox();
            this.CreateMappingbutton = new System.Windows.Forms.Button();
            this.GetEntitesbutton = new System.Windows.Forms.Button();
            this.Savebutton = new System.Windows.Forms.Button();
            this.SyncMappingbutton = new System.Windows.Forms.Button();
            this.CreateFileMappingbutton = new System.Windows.Forms.Button();
            entity1DataSourceLabel = new System.Windows.Forms.Label();
            entityName1Label = new System.Windows.Forms.Label();
            entity2DataSourceLabel = new System.Windows.Forms.Label();
            entityName2Label = new System.Windows.Forms.Label();
            mappingNameLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.fldMappingBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.mappingsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fldMappingDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // entity1DataSourceLabel
            // 
            entity1DataSourceLabel.AutoSize = true;
            entity1DataSourceLabel.Font = new System.Drawing.Font("MS UI Gothic", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            entity1DataSourceLabel.Location = new System.Drawing.Point(194, 109);
            entity1DataSourceLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            entity1DataSourceLabel.Name = "entity1DataSourceLabel";
            entity1DataSourceLabel.Size = new System.Drawing.Size(144, 14);
            entity1DataSourceLabel.TabIndex = 6;
            entity1DataSourceLabel.Text = "Entity1Data Source:";
            // 
            // entityName1Label
            // 
            entityName1Label.AutoSize = true;
            entityName1Label.Font = new System.Drawing.Font("MS UI Gothic", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            entityName1Label.Location = new System.Drawing.Point(593, 109);
            entityName1Label.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            entityName1Label.Name = "entityName1Label";
            entityName1Label.Size = new System.Drawing.Size(102, 14);
            entityName1Label.TabIndex = 7;
            entityName1Label.Text = "Entity Name1:";
            // 
            // entity2DataSourceLabel
            // 
            entity2DataSourceLabel.AutoSize = true;
            entity2DataSourceLabel.Font = new System.Drawing.Font("MS UI Gothic", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            entity2DataSourceLabel.Location = new System.Drawing.Point(194, 144);
            entity2DataSourceLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            entity2DataSourceLabel.Name = "entity2DataSourceLabel";
            entity2DataSourceLabel.Size = new System.Drawing.Size(144, 14);
            entity2DataSourceLabel.TabIndex = 8;
            entity2DataSourceLabel.Text = "Entity2Data Source:";
            // 
            // entityName2Label
            // 
            entityName2Label.AutoSize = true;
            entityName2Label.Font = new System.Drawing.Font("MS UI Gothic", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            entityName2Label.Location = new System.Drawing.Point(593, 144);
            entityName2Label.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            entityName2Label.Name = "entityName2Label";
            entityName2Label.Size = new System.Drawing.Size(102, 14);
            entityName2Label.TabIndex = 9;
            entityName2Label.Text = "Entity Name2:";
            // 
            // mappingNameLabel
            // 
            mappingNameLabel.AutoSize = true;
            mappingNameLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            mappingNameLabel.Location = new System.Drawing.Point(232, 54);
            mappingNameLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            mappingNameLabel.Name = "mappingNameLabel";
            mappingNameLabel.Size = new System.Drawing.Size(141, 22);
            mappingNameLabel.TabIndex = 10;
            mappingNameLabel.Text = "Mapping Name:";
            // 
            // fldMappingBindingSource
            // 
            this.fldMappingBindingSource.DataMember = "FldMapping";
            this.fldMappingBindingSource.DataSource = this.mappingsBindingSource;
            // 
            // mappingsBindingSource
            // 
            this.mappingsBindingSource.AllowNew = true;
            this.mappingsBindingSource.DataSource = typeof(TheTechIdea.DataManagment_Engine.Workflow.IMapping_rep);
            // 
            // fldMappingDataGridView
            // 
            this.fldMappingDataGridView.AutoGenerateColumns = false;
            this.fldMappingDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.fldMappingDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.FieldName1Comobobox,
            this.FieldType1Comobobox,
            this.dataGridViewTextBoxColumn3,
            this.FieldName2Comobobox,
            this.FieldType2Comobobox,
            this.dataGridViewTextBoxColumn6,
            this.dataGridViewTextBoxColumn7});
            this.fldMappingDataGridView.DataSource = this.fldMappingBindingSource;
            this.fldMappingDataGridView.Location = new System.Drawing.Point(196, 258);
            this.fldMappingDataGridView.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.fldMappingDataGridView.Name = "fldMappingDataGridView";
            this.fldMappingDataGridView.RowHeadersWidth = 62;
            this.fldMappingDataGridView.RowTemplate.Height = 28;
            this.fldMappingDataGridView.Size = new System.Drawing.Size(576, 332);
            this.fldMappingDataGridView.TabIndex = 5;
            // 
            // FieldName1Comobobox
            // 
            this.FieldName1Comobobox.DataPropertyName = "FieldName1";
            this.FieldName1Comobobox.HeaderText = "Field Name 1";
            this.FieldName1Comobobox.MinimumWidth = 8;
            this.FieldName1Comobobox.Name = "FieldName1Comobobox";
            this.FieldName1Comobobox.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.FieldName1Comobobox.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.FieldName1Comobobox.Width = 150;
            // 
            // FieldType1Comobobox
            // 
            this.FieldType1Comobobox.DataPropertyName = "FieldType1";
            this.FieldType1Comobobox.HeaderText = "Field Type 1";
            this.FieldType1Comobobox.MinimumWidth = 8;
            this.FieldType1Comobobox.Name = "FieldType1Comobobox";
            this.FieldType1Comobobox.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.FieldType1Comobobox.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.FieldType1Comobobox.Visible = false;
            this.FieldType1Comobobox.Width = 150;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "FieldIndex1";
            this.dataGridViewTextBoxColumn3.HeaderText = "FieldIndex1";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 8;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.Visible = false;
            this.dataGridViewTextBoxColumn3.Width = 150;
            // 
            // FieldName2Comobobox
            // 
            this.FieldName2Comobobox.DataPropertyName = "FieldName2";
            this.FieldName2Comobobox.HeaderText = "Field Name 2";
            this.FieldName2Comobobox.MinimumWidth = 8;
            this.FieldName2Comobobox.Name = "FieldName2Comobobox";
            this.FieldName2Comobobox.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.FieldName2Comobobox.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.FieldName2Comobobox.Width = 150;
            // 
            // FieldType2Comobobox
            // 
            this.FieldType2Comobobox.DataPropertyName = "FieldType2";
            this.FieldType2Comobobox.HeaderText = "Field Type 2";
            this.FieldType2Comobobox.MinimumWidth = 8;
            this.FieldType2Comobobox.Name = "FieldType2Comobobox";
            this.FieldType2Comobobox.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.FieldType2Comobobox.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.FieldType2Comobobox.Visible = false;
            this.FieldType2Comobobox.Width = 150;
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.DataPropertyName = "FieldIndex2";
            this.dataGridViewTextBoxColumn6.HeaderText = "FieldIndex2";
            this.dataGridViewTextBoxColumn6.MinimumWidth = 8;
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.Visible = false;
            this.dataGridViewTextBoxColumn6.Width = 150;
            // 
            // dataGridViewTextBoxColumn7
            // 
            this.dataGridViewTextBoxColumn7.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.dataGridViewTextBoxColumn7.DataPropertyName = "Rules";
            this.dataGridViewTextBoxColumn7.HeaderText = "Rules";
            this.dataGridViewTextBoxColumn7.MinimumWidth = 8;
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            // 
            // entity1DataSourceComboBox
            // 
            this.entity1DataSourceComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.mappingsBindingSource, "Entity1DataSource", true));
            this.entity1DataSourceComboBox.Font = new System.Drawing.Font("MS UI Gothic", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.entity1DataSourceComboBox.FormattingEnabled = true;
            this.entity1DataSourceComboBox.Location = new System.Drawing.Point(338, 107);
            this.entity1DataSourceComboBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.entity1DataSourceComboBox.Name = "entity1DataSourceComboBox";
            this.entity1DataSourceComboBox.Size = new System.Drawing.Size(123, 21);
            this.entity1DataSourceComboBox.TabIndex = 7;
            // 
            // entityName1ComboBox
            // 
            this.entityName1ComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.mappingsBindingSource, "EntityName1", true));
            this.entityName1ComboBox.Font = new System.Drawing.Font("MS UI Gothic", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.entityName1ComboBox.FormattingEnabled = true;
            this.entityName1ComboBox.Location = new System.Drawing.Point(698, 107);
            this.entityName1ComboBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.entityName1ComboBox.Name = "entityName1ComboBox";
            this.entityName1ComboBox.Size = new System.Drawing.Size(127, 21);
            this.entityName1ComboBox.TabIndex = 8;
            // 
            // entity2DataSourceComboBox
            // 
            this.entity2DataSourceComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.mappingsBindingSource, "Entity2DataSource", true));
            this.entity2DataSourceComboBox.Font = new System.Drawing.Font("MS UI Gothic", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.entity2DataSourceComboBox.FormattingEnabled = true;
            this.entity2DataSourceComboBox.Location = new System.Drawing.Point(338, 142);
            this.entity2DataSourceComboBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.entity2DataSourceComboBox.Name = "entity2DataSourceComboBox";
            this.entity2DataSourceComboBox.Size = new System.Drawing.Size(123, 21);
            this.entity2DataSourceComboBox.TabIndex = 9;
            // 
            // entityName2ComboBox
            // 
            this.entityName2ComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.mappingsBindingSource, "EntityName2", true));
            this.entityName2ComboBox.Font = new System.Drawing.Font("MS UI Gothic", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.entityName2ComboBox.FormattingEnabled = true;
            this.entityName2ComboBox.Location = new System.Drawing.Point(698, 142);
            this.entityName2ComboBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.entityName2ComboBox.Name = "entityName2ComboBox";
            this.entityName2ComboBox.Size = new System.Drawing.Size(127, 21);
            this.entityName2ComboBox.TabIndex = 10;
            // 
            // mappingNameTextBox
            // 
            this.mappingNameTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.mappingsBindingSource, "MappingName", true));
            this.mappingNameTextBox.Location = new System.Drawing.Point(376, 58);
            this.mappingNameTextBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.mappingNameTextBox.Name = "mappingNameTextBox";
            this.mappingNameTextBox.Size = new System.Drawing.Size(328, 20);
            this.mappingNameTextBox.TabIndex = 11;
            // 
            // CreateMappingbutton
            // 
            this.CreateMappingbutton.Location = new System.Drawing.Point(776, 215);
            this.CreateMappingbutton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.CreateMappingbutton.Name = "CreateMappingbutton";
            this.CreateMappingbutton.Size = new System.Drawing.Size(122, 30);
            this.CreateMappingbutton.TabIndex = 12;
            this.CreateMappingbutton.Text = "Create Mapping";
            this.CreateMappingbutton.UseVisualStyleBackColor = true;
            this.CreateMappingbutton.Click += new System.EventHandler(this.CreateMappingbutton_Click);
            // 
            // GetEntitesbutton
            // 
            this.GetEntitesbutton.Location = new System.Drawing.Point(475, 119);
            this.GetEntitesbutton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.GetEntitesbutton.Name = "GetEntitesbutton";
            this.GetEntitesbutton.Size = new System.Drawing.Size(93, 31);
            this.GetEntitesbutton.TabIndex = 13;
            this.GetEntitesbutton.Text = "Get Entities";
            this.GetEntitesbutton.UseVisualStyleBackColor = true;
            this.GetEntitesbutton.Click += new System.EventHandler(this.GetEntitesbutton_Click);
            // 
            // Savebutton
            // 
            this.Savebutton.Location = new System.Drawing.Point(776, 559);
            this.Savebutton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Savebutton.Name = "Savebutton";
            this.Savebutton.Size = new System.Drawing.Size(122, 31);
            this.Savebutton.TabIndex = 14;
            this.Savebutton.Text = "Save Mapping";
            this.Savebutton.UseVisualStyleBackColor = true;
            this.Savebutton.Click += new System.EventHandler(this.Savebutton_Click);
            // 
            // SyncMappingbutton
            // 
            this.SyncMappingbutton.Location = new System.Drawing.Point(776, 181);
            this.SyncMappingbutton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.SyncMappingbutton.Name = "SyncMappingbutton";
            this.SyncMappingbutton.Size = new System.Drawing.Size(122, 31);
            this.SyncMappingbutton.TabIndex = 15;
            this.SyncMappingbutton.Text = "Create Sync Mapping";
            this.SyncMappingbutton.UseVisualStyleBackColor = true;
            // 
            // CreateFileMappingbutton
            // 
            this.CreateFileMappingbutton.AllowDrop = true;
            this.CreateFileMappingbutton.Location = new System.Drawing.Point(776, 258);
            this.CreateFileMappingbutton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.CreateFileMappingbutton.Name = "CreateFileMappingbutton";
            this.CreateFileMappingbutton.Size = new System.Drawing.Size(122, 30);
            this.CreateFileMappingbutton.TabIndex = 17;
            this.CreateFileMappingbutton.Text = "Create File Mapping";
            this.CreateFileMappingbutton.UseVisualStyleBackColor = true;
            // 
            // uc_MappingEntities
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.CreateFileMappingbutton);
            this.Controls.Add(this.SyncMappingbutton);
            this.Controls.Add(this.Savebutton);
            this.Controls.Add(this.GetEntitesbutton);
            this.Controls.Add(this.CreateMappingbutton);
            this.Controls.Add(mappingNameLabel);
            this.Controls.Add(this.mappingNameTextBox);
            this.Controls.Add(entityName2Label);
            this.Controls.Add(this.entityName2ComboBox);
            this.Controls.Add(entity2DataSourceLabel);
            this.Controls.Add(this.entity2DataSourceComboBox);
            this.Controls.Add(entityName1Label);
            this.Controls.Add(this.entityName1ComboBox);
            this.Controls.Add(entity1DataSourceLabel);
            this.Controls.Add(this.entity1DataSourceComboBox);
            this.Controls.Add(this.fldMappingDataGridView);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "uc_MappingEntities";
            this.Size = new System.Drawing.Size(937, 679);
            ((System.ComponentModel.ISupportInitialize)(this.fldMappingBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.mappingsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fldMappingDataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.BindingSource mappingsBindingSource;
        private System.Windows.Forms.BindingSource fldMappingBindingSource;
        private System.Windows.Forms.DataGridView fldMappingDataGridView;
        private System.Windows.Forms.ComboBox entity1DataSourceComboBox;
        private System.Windows.Forms.ComboBox entityName1ComboBox;
        private System.Windows.Forms.ComboBox entity2DataSourceComboBox;
        private System.Windows.Forms.ComboBox entityName2ComboBox;
        private System.Windows.Forms.TextBox mappingNameTextBox;
        private System.Windows.Forms.Button CreateMappingbutton;
        private System.Windows.Forms.Button GetEntitesbutton;
        private System.Windows.Forms.Button Savebutton;
        private System.Windows.Forms.DataGridViewComboBoxColumn FieldName1Comobobox;
        private System.Windows.Forms.DataGridViewComboBoxColumn FieldType1Comobobox;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewComboBoxColumn FieldName2Comobobox;
        private System.Windows.Forms.DataGridViewComboBoxColumn FieldType2Comobobox;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private System.Windows.Forms.Button SyncMappingbutton;
        private System.Windows.Forms.Button CreateFileMappingbutton;
    }
}
