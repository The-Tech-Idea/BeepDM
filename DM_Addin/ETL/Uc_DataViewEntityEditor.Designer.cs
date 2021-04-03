namespace TheTechIdea.ETL
{
    partial class Uc_DataViewEntityEditor
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
            System.Windows.Forms.Label dataSourceIDLabel;
            System.Windows.Forms.Label viewtypeLabel;
            System.Windows.Forms.Label nameLabel;
            System.Windows.Forms.Label customBuildQueryLabel;
            System.Windows.Forms.Label viewIDLabel;
            System.Windows.Forms.Label label1;
            System.Windows.Forms.Label label2;
            this.dataSourceIDComboBox = new System.Windows.Forms.ComboBox();
            this.dataHierarchyBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.viewtypeComboBox = new System.Windows.Forms.ComboBox();
            this.nameTextBox = new System.Windows.Forms.TextBox();
            this.customBuildQueryTextBox = new System.Windows.Forms.TextBox();
            this.ValidateQuerybutton = new System.Windows.Forms.Button();
            this.fieldsDataGridView = new System.Windows.Forms.DataGridView();
            this.fieldsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.relationShipsDataGridView = new System.Windows.Forms.DataGridView();
            this.parentEntityIDDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.parentEntityColumnIDDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.entityColumnIDDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.relationShipsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.ValidateFKbutton = new System.Windows.Forms.Button();
            this.viewIDTextBox = new System.Windows.Forms.TextBox();
            this.primaryKeysBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.SaveEntitybutton = new System.Windows.Forms.Button();
            this.CustomQueryDatadataGridView = new System.Windows.Forms.DataGridView();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.ValidateFieldsbutton = new System.Windows.Forms.Button();
            this.idDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.fieldnameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.fieldtypeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.size1DataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.size2DataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.fieldCategoryDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.isAutoIncrementDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.allowDBNullDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.isCheckDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.isUniqueDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.isKeyDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.fieldIndexDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.entityNameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataSourceIDLabel = new System.Windows.Forms.Label();
            viewtypeLabel = new System.Windows.Forms.Label();
            nameLabel = new System.Windows.Forms.Label();
            customBuildQueryLabel = new System.Windows.Forms.Label();
            viewIDLabel = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataHierarchyBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fieldsDataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fieldsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.relationShipsDataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.relationShipsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.primaryKeysBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CustomQueryDatadataGridView)).BeginInit();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataSourceIDLabel
            // 
            dataSourceIDLabel.AutoSize = true;
            dataSourceIDLabel.Location = new System.Drawing.Point(38, 50);
            dataSourceIDLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            dataSourceIDLabel.Name = "dataSourceIDLabel";
            dataSourceIDLabel.Size = new System.Drawing.Size(84, 13);
            dataSourceIDLabel.TabIndex = 1;
            dataSourceIDLabel.Text = "Data Source ID:";
            // 
            // viewtypeLabel
            // 
            viewtypeLabel.AutoSize = true;
            viewtypeLabel.Location = new System.Drawing.Point(69, 99);
            viewtypeLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            viewtypeLabel.Name = "viewtypeLabel";
            viewtypeLabel.Size = new System.Drawing.Size(53, 13);
            viewtypeLabel.TabIndex = 2;
            viewtypeLabel.Text = "Viewtype:";
            // 
            // nameLabel
            // 
            nameLabel.AutoSize = true;
            nameLabel.Location = new System.Drawing.Point(83, 26);
            nameLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            nameLabel.Name = "nameLabel";
            nameLabel.Size = new System.Drawing.Size(38, 13);
            nameLabel.TabIndex = 4;
            nameLabel.Text = "Name:";
            // 
            // customBuildQueryLabel
            // 
            customBuildQueryLabel.AutoSize = true;
            customBuildQueryLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(254)));
            customBuildQueryLabel.Location = new System.Drawing.Point(2, 0);
            customBuildQueryLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            customBuildQueryLabel.Name = "customBuildQueryLabel";
            customBuildQueryLabel.Size = new System.Drawing.Size(121, 13);
            customBuildQueryLabel.TabIndex = 6;
            customBuildQueryLabel.Text = "Custom Build Query:";
            // 
            // viewIDLabel
            // 
            viewIDLabel.AutoSize = true;
            viewIDLabel.Location = new System.Drawing.Point(75, 75);
            viewIDLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            viewIDLabel.Name = "viewIDLabel";
            viewIDLabel.Size = new System.Drawing.Size(47, 13);
            viewIDLabel.TabIndex = 12;
            viewIDLabel.Text = "View ID:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(254)));
            label1.Location = new System.Drawing.Point(8, 15);
            label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(64, 13);
            label1.TabIndex = 15;
            label1.Text = "Relations:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(254)));
            label2.Location = new System.Drawing.Point(15, 10);
            label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(44, 13);
            label2.TabIndex = 16;
            label2.Text = "Fields:";
            // 
            // dataSourceIDComboBox
            // 
            this.dataSourceIDComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.dataHierarchyBindingSource, "DataSourceID", true));
            this.dataSourceIDComboBox.FormattingEnabled = true;
            this.dataSourceIDComboBox.Location = new System.Drawing.Point(126, 47);
            this.dataSourceIDComboBox.Margin = new System.Windows.Forms.Padding(2);
            this.dataSourceIDComboBox.Name = "dataSourceIDComboBox";
            this.dataSourceIDComboBox.Size = new System.Drawing.Size(200, 21);
            this.dataSourceIDComboBox.TabIndex = 2;
            // 
            // dataHierarchyBindingSource
            // 
            this.dataHierarchyBindingSource.DataSource = typeof(TheTechIdea.DataManagment_Engine.DataBase.EntityStructure);
            // 
            // viewtypeComboBox
            // 
            this.viewtypeComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.dataHierarchyBindingSource, "Viewtype", true));
            this.viewtypeComboBox.FormattingEnabled = true;
            this.viewtypeComboBox.Location = new System.Drawing.Point(126, 96);
            this.viewtypeComboBox.Margin = new System.Windows.Forms.Padding(2);
            this.viewtypeComboBox.Name = "viewtypeComboBox";
            this.viewtypeComboBox.Size = new System.Drawing.Size(200, 21);
            this.viewtypeComboBox.TabIndex = 3;
            // 
            // nameTextBox
            // 
            this.nameTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.dataHierarchyBindingSource, "EntityName", true));
            this.nameTextBox.Location = new System.Drawing.Point(125, 23);
            this.nameTextBox.Margin = new System.Windows.Forms.Padding(2);
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.Size = new System.Drawing.Size(396, 20);
            this.nameTextBox.TabIndex = 5;
            // 
            // customBuildQueryTextBox
            // 
            this.customBuildQueryTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.dataHierarchyBindingSource, "CustomBuildQuery", true));
            this.customBuildQueryTextBox.Location = new System.Drawing.Point(3, 29);
            this.customBuildQueryTextBox.Margin = new System.Windows.Forms.Padding(2);
            this.customBuildQueryTextBox.Multiline = true;
            this.customBuildQueryTextBox.Name = "customBuildQueryTextBox";
            this.customBuildQueryTextBox.Size = new System.Drawing.Size(455, 200);
            this.customBuildQueryTextBox.TabIndex = 7;
            // 
            // ValidateQuerybutton
            // 
            this.ValidateQuerybutton.Location = new System.Drawing.Point(362, 2);
            this.ValidateQuerybutton.Margin = new System.Windows.Forms.Padding(2);
            this.ValidateQuerybutton.Name = "ValidateQuerybutton";
            this.ValidateQuerybutton.Size = new System.Drawing.Size(99, 23);
            this.ValidateQuerybutton.TabIndex = 8;
            this.ValidateQuerybutton.Text = "Validate Query";
            this.ValidateQuerybutton.UseVisualStyleBackColor = true;
            // 
            // fieldsDataGridView
            // 
            this.fieldsDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fieldsDataGridView.AutoGenerateColumns = false;
            this.fieldsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.fieldsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.idDataGridViewTextBoxColumn,
            this.fieldnameDataGridViewTextBoxColumn,
            this.fieldtypeDataGridViewTextBoxColumn,
            this.size1DataGridViewTextBoxColumn,
            this.size2DataGridViewTextBoxColumn,
            this.fieldCategoryDataGridViewTextBoxColumn,
            this.isAutoIncrementDataGridViewCheckBoxColumn,
            this.allowDBNullDataGridViewCheckBoxColumn,
            this.isCheckDataGridViewCheckBoxColumn,
            this.isUniqueDataGridViewCheckBoxColumn,
            this.isKeyDataGridViewCheckBoxColumn,
            this.fieldIndexDataGridViewTextBoxColumn,
            this.entityNameDataGridViewTextBoxColumn});
            this.fieldsDataGridView.DataSource = this.fieldsBindingSource;
            this.fieldsDataGridView.Location = new System.Drawing.Point(11, 36);
            this.fieldsDataGridView.Margin = new System.Windows.Forms.Padding(2);
            this.fieldsDataGridView.Name = "fieldsDataGridView";
            this.fieldsDataGridView.RowHeadersWidth = 62;
            this.fieldsDataGridView.RowTemplate.Height = 28;
            this.fieldsDataGridView.Size = new System.Drawing.Size(498, 211);
            this.fieldsDataGridView.TabIndex = 9;
            // 
            // fieldsBindingSource
            // 
            this.fieldsBindingSource.DataMember = "Fields";
            this.fieldsBindingSource.DataSource = this.dataHierarchyBindingSource;
            // 
            // relationShipsDataGridView
            // 
            this.relationShipsDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.relationShipsDataGridView.AutoGenerateColumns = false;
            this.relationShipsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.relationShipsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.parentEntityIDDataGridViewTextBoxColumn,
            this.parentEntityColumnIDDataGridViewTextBoxColumn,
            this.entityColumnIDDataGridViewTextBoxColumn});
            this.relationShipsDataGridView.DataSource = this.relationShipsBindingSource;
            this.relationShipsDataGridView.Location = new System.Drawing.Point(11, 37);
            this.relationShipsDataGridView.Margin = new System.Windows.Forms.Padding(2);
            this.relationShipsDataGridView.Name = "relationShipsDataGridView";
            this.relationShipsDataGridView.RowHeadersWidth = 62;
            this.relationShipsDataGridView.RowTemplate.Height = 28;
            this.relationShipsDataGridView.Size = new System.Drawing.Size(498, 175);
            this.relationShipsDataGridView.TabIndex = 10;
            // 
            // parentEntityIDDataGridViewTextBoxColumn
            // 
            this.parentEntityIDDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.parentEntityIDDataGridViewTextBoxColumn.DataPropertyName = "ParentEntityID";
            this.parentEntityIDDataGridViewTextBoxColumn.HeaderText = "ParentEntityID";
            this.parentEntityIDDataGridViewTextBoxColumn.MinimumWidth = 8;
            this.parentEntityIDDataGridViewTextBoxColumn.Name = "parentEntityIDDataGridViewTextBoxColumn";
            // 
            // parentEntityColumnIDDataGridViewTextBoxColumn
            // 
            this.parentEntityColumnIDDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.parentEntityColumnIDDataGridViewTextBoxColumn.DataPropertyName = "ParentEntityColumnID";
            this.parentEntityColumnIDDataGridViewTextBoxColumn.HeaderText = "ParentEntityColumnID";
            this.parentEntityColumnIDDataGridViewTextBoxColumn.MinimumWidth = 8;
            this.parentEntityColumnIDDataGridViewTextBoxColumn.Name = "parentEntityColumnIDDataGridViewTextBoxColumn";
            // 
            // entityColumnIDDataGridViewTextBoxColumn
            // 
            this.entityColumnIDDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.entityColumnIDDataGridViewTextBoxColumn.DataPropertyName = "EntityColumnID";
            this.entityColumnIDDataGridViewTextBoxColumn.HeaderText = "EntityColumnID";
            this.entityColumnIDDataGridViewTextBoxColumn.MinimumWidth = 8;
            this.entityColumnIDDataGridViewTextBoxColumn.Name = "entityColumnIDDataGridViewTextBoxColumn";
            // 
            // relationShipsBindingSource
            // 
            this.relationShipsBindingSource.DataMember = "Relations";
            this.relationShipsBindingSource.DataSource = this.dataHierarchyBindingSource;
            // 
            // ValidateFKbutton
            // 
            this.ValidateFKbutton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ValidateFKbutton.Location = new System.Drawing.Point(379, 5);
            this.ValidateFKbutton.Margin = new System.Windows.Forms.Padding(2);
            this.ValidateFKbutton.Name = "ValidateFKbutton";
            this.ValidateFKbutton.Size = new System.Drawing.Size(130, 23);
            this.ValidateFKbutton.TabIndex = 11;
            this.ValidateFKbutton.Text = "Validate FK Relations";
            this.ValidateFKbutton.UseVisualStyleBackColor = true;
            // 
            // viewIDTextBox
            // 
            this.viewIDTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.dataHierarchyBindingSource, "ViewID", true));
            this.viewIDTextBox.Location = new System.Drawing.Point(126, 72);
            this.viewIDTextBox.Margin = new System.Windows.Forms.Padding(2);
            this.viewIDTextBox.Name = "viewIDTextBox";
            this.viewIDTextBox.ReadOnly = true;
            this.viewIDTextBox.Size = new System.Drawing.Size(68, 20);
            this.viewIDTextBox.TabIndex = 13;
            // 
            // primaryKeysBindingSource
            // 
            this.primaryKeysBindingSource.DataMember = "PrimaryKeys";
            this.primaryKeysBindingSource.DataSource = this.dataHierarchyBindingSource;
            // 
            // SaveEntitybutton
            // 
            this.SaveEntitybutton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(254)));
            this.SaveEntitybutton.Location = new System.Drawing.Point(267, 143);
            this.SaveEntitybutton.Margin = new System.Windows.Forms.Padding(2);
            this.SaveEntitybutton.Name = "SaveEntitybutton";
            this.SaveEntitybutton.Size = new System.Drawing.Size(99, 23);
            this.SaveEntitybutton.TabIndex = 14;
            this.SaveEntitybutton.Text = "Save";
            this.SaveEntitybutton.UseVisualStyleBackColor = true;
            // 
            // CustomQueryDatadataGridView
            // 
            this.CustomQueryDatadataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.CustomQueryDatadataGridView.Location = new System.Drawing.Point(3, 234);
            this.CustomQueryDatadataGridView.Name = "CustomQueryDatadataGridView";
            this.CustomQueryDatadataGridView.Size = new System.Drawing.Size(455, 437);
            this.CustomQueryDatadataGridView.TabIndex = 17;
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.customBuildQueryTextBox);
            this.panel1.Controls.Add(this.CustomQueryDatadataGridView);
            this.panel1.Controls.Add(this.ValidateQuerybutton);
            this.panel1.Controls.Add(customBuildQueryLabel);
            this.panel1.Location = new System.Drawing.Point(528, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(462, 676);
            this.panel1.TabIndex = 18;
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.relationShipsDataGridView);
            this.panel2.Controls.Add(this.ValidateFKbutton);
            this.panel2.Controls.Add(label1);
            this.panel2.Location = new System.Drawing.Point(3, 451);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(518, 228);
            this.panel2.TabIndex = 19;
            // 
            // panel3
            // 
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.fieldsDataGridView);
            this.panel3.Controls.Add(label2);
            this.panel3.Location = new System.Drawing.Point(3, 194);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(519, 251);
            this.panel3.TabIndex = 18;
            // 
            // ValidateFieldsbutton
            // 
            this.ValidateFieldsbutton.Location = new System.Drawing.Point(414, 166);
            this.ValidateFieldsbutton.Margin = new System.Windows.Forms.Padding(2);
            this.ValidateFieldsbutton.Name = "ValidateFieldsbutton";
            this.ValidateFieldsbutton.Size = new System.Drawing.Size(99, 23);
            this.ValidateFieldsbutton.TabIndex = 18;
            this.ValidateFieldsbutton.Text = "Validate Fields";
            this.ValidateFieldsbutton.UseVisualStyleBackColor = true;
            // 
            // idDataGridViewTextBoxColumn
            // 
            this.idDataGridViewTextBoxColumn.DataPropertyName = "id";
            this.idDataGridViewTextBoxColumn.HeaderText = "id";
            this.idDataGridViewTextBoxColumn.MinimumWidth = 8;
            this.idDataGridViewTextBoxColumn.Name = "idDataGridViewTextBoxColumn";
            this.idDataGridViewTextBoxColumn.Visible = false;
            this.idDataGridViewTextBoxColumn.Width = 150;
            // 
            // fieldnameDataGridViewTextBoxColumn
            // 
            this.fieldnameDataGridViewTextBoxColumn.DataPropertyName = "fieldname";
            this.fieldnameDataGridViewTextBoxColumn.HeaderText = "fieldname";
            this.fieldnameDataGridViewTextBoxColumn.MinimumWidth = 8;
            this.fieldnameDataGridViewTextBoxColumn.Name = "fieldnameDataGridViewTextBoxColumn";
            this.fieldnameDataGridViewTextBoxColumn.Width = 150;
            // 
            // fieldtypeDataGridViewTextBoxColumn
            // 
            this.fieldtypeDataGridViewTextBoxColumn.DataPropertyName = "fieldtype";
            this.fieldtypeDataGridViewTextBoxColumn.HeaderText = "fieldtype";
            this.fieldtypeDataGridViewTextBoxColumn.MinimumWidth = 8;
            this.fieldtypeDataGridViewTextBoxColumn.Name = "fieldtypeDataGridViewTextBoxColumn";
            this.fieldtypeDataGridViewTextBoxColumn.Width = 150;
            // 
            // size1DataGridViewTextBoxColumn
            // 
            this.size1DataGridViewTextBoxColumn.DataPropertyName = "Size1";
            this.size1DataGridViewTextBoxColumn.HeaderText = "Size1";
            this.size1DataGridViewTextBoxColumn.MinimumWidth = 8;
            this.size1DataGridViewTextBoxColumn.Name = "size1DataGridViewTextBoxColumn";
            this.size1DataGridViewTextBoxColumn.Width = 150;
            // 
            // size2DataGridViewTextBoxColumn
            // 
            this.size2DataGridViewTextBoxColumn.DataPropertyName = "Size2";
            this.size2DataGridViewTextBoxColumn.HeaderText = "Size2";
            this.size2DataGridViewTextBoxColumn.MinimumWidth = 8;
            this.size2DataGridViewTextBoxColumn.Name = "size2DataGridViewTextBoxColumn";
            this.size2DataGridViewTextBoxColumn.Width = 150;
            // 
            // fieldCategoryDataGridViewTextBoxColumn
            // 
            this.fieldCategoryDataGridViewTextBoxColumn.DataPropertyName = "fieldCategory";
            this.fieldCategoryDataGridViewTextBoxColumn.HeaderText = "fieldCategory";
            this.fieldCategoryDataGridViewTextBoxColumn.MinimumWidth = 8;
            this.fieldCategoryDataGridViewTextBoxColumn.Name = "fieldCategoryDataGridViewTextBoxColumn";
            this.fieldCategoryDataGridViewTextBoxColumn.Visible = false;
            this.fieldCategoryDataGridViewTextBoxColumn.Width = 150;
            // 
            // isAutoIncrementDataGridViewCheckBoxColumn
            // 
            this.isAutoIncrementDataGridViewCheckBoxColumn.DataPropertyName = "IsAutoIncrement";
            this.isAutoIncrementDataGridViewCheckBoxColumn.HeaderText = "IsAutoIncrement";
            this.isAutoIncrementDataGridViewCheckBoxColumn.MinimumWidth = 8;
            this.isAutoIncrementDataGridViewCheckBoxColumn.Name = "isAutoIncrementDataGridViewCheckBoxColumn";
            this.isAutoIncrementDataGridViewCheckBoxColumn.Width = 150;
            // 
            // allowDBNullDataGridViewCheckBoxColumn
            // 
            this.allowDBNullDataGridViewCheckBoxColumn.DataPropertyName = "AllowDBNull";
            this.allowDBNullDataGridViewCheckBoxColumn.HeaderText = "AllowDBNull";
            this.allowDBNullDataGridViewCheckBoxColumn.MinimumWidth = 8;
            this.allowDBNullDataGridViewCheckBoxColumn.Name = "allowDBNullDataGridViewCheckBoxColumn";
            this.allowDBNullDataGridViewCheckBoxColumn.Width = 150;
            // 
            // isCheckDataGridViewCheckBoxColumn
            // 
            this.isCheckDataGridViewCheckBoxColumn.DataPropertyName = "IsCheck";
            this.isCheckDataGridViewCheckBoxColumn.HeaderText = "IsCheck";
            this.isCheckDataGridViewCheckBoxColumn.MinimumWidth = 8;
            this.isCheckDataGridViewCheckBoxColumn.Name = "isCheckDataGridViewCheckBoxColumn";
            this.isCheckDataGridViewCheckBoxColumn.Visible = false;
            this.isCheckDataGridViewCheckBoxColumn.Width = 150;
            // 
            // isUniqueDataGridViewCheckBoxColumn
            // 
            this.isUniqueDataGridViewCheckBoxColumn.DataPropertyName = "IsUnique";
            this.isUniqueDataGridViewCheckBoxColumn.HeaderText = "IsUnique";
            this.isUniqueDataGridViewCheckBoxColumn.MinimumWidth = 8;
            this.isUniqueDataGridViewCheckBoxColumn.Name = "isUniqueDataGridViewCheckBoxColumn";
            this.isUniqueDataGridViewCheckBoxColumn.Width = 150;
            // 
            // isKeyDataGridViewCheckBoxColumn
            // 
            this.isKeyDataGridViewCheckBoxColumn.DataPropertyName = "IsKey";
            this.isKeyDataGridViewCheckBoxColumn.HeaderText = "IsKey";
            this.isKeyDataGridViewCheckBoxColumn.MinimumWidth = 8;
            this.isKeyDataGridViewCheckBoxColumn.Name = "isKeyDataGridViewCheckBoxColumn";
            this.isKeyDataGridViewCheckBoxColumn.Width = 150;
            // 
            // fieldIndexDataGridViewTextBoxColumn
            // 
            this.fieldIndexDataGridViewTextBoxColumn.DataPropertyName = "FieldIndex";
            this.fieldIndexDataGridViewTextBoxColumn.HeaderText = "FieldIndex";
            this.fieldIndexDataGridViewTextBoxColumn.MinimumWidth = 8;
            this.fieldIndexDataGridViewTextBoxColumn.Name = "fieldIndexDataGridViewTextBoxColumn";
            this.fieldIndexDataGridViewTextBoxColumn.Visible = false;
            this.fieldIndexDataGridViewTextBoxColumn.Width = 150;
            // 
            // entityNameDataGridViewTextBoxColumn
            // 
            this.entityNameDataGridViewTextBoxColumn.DataPropertyName = "EntityName";
            this.entityNameDataGridViewTextBoxColumn.HeaderText = "EntityName";
            this.entityNameDataGridViewTextBoxColumn.MinimumWidth = 8;
            this.entityNameDataGridViewTextBoxColumn.Name = "entityNameDataGridViewTextBoxColumn";
            this.entityNameDataGridViewTextBoxColumn.Visible = false;
            this.entityNameDataGridViewTextBoxColumn.Width = 150;
            // 
            // Uc_DataViewEntityEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.ValidateFieldsbutton);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.SaveEntitybutton);
            this.Controls.Add(viewIDLabel);
            this.Controls.Add(this.viewIDTextBox);
            this.Controls.Add(nameLabel);
            this.Controls.Add(this.nameTextBox);
            this.Controls.Add(viewtypeLabel);
            this.Controls.Add(this.viewtypeComboBox);
            this.Controls.Add(dataSourceIDLabel);
            this.Controls.Add(this.dataSourceIDComboBox);
            this.Name = "Uc_DataViewEntityEditor";
            this.Size = new System.Drawing.Size(993, 682);
            ((System.ComponentModel.ISupportInitialize)(this.dataHierarchyBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fieldsDataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fieldsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.relationShipsDataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.relationShipsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.primaryKeysBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CustomQueryDatadataGridView)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.BindingSource dataHierarchyBindingSource;
        private System.Windows.Forms.ComboBox dataSourceIDComboBox;
        private System.Windows.Forms.ComboBox viewtypeComboBox;
        private System.Windows.Forms.TextBox nameTextBox;
        private System.Windows.Forms.TextBox customBuildQueryTextBox;
        private System.Windows.Forms.Button ValidateQuerybutton;
        private System.Windows.Forms.BindingSource fieldsBindingSource;
        private System.Windows.Forms.DataGridView fieldsDataGridView;
      //  private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
      //  private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
      //  private System.Windows.Forms.DataGridViewCheckBoxColumn dataGridViewCheckBoxColumn3;
        private System.Windows.Forms.BindingSource relationShipsBindingSource;
        private System.Windows.Forms.DataGridView relationShipsDataGridView;
      //  private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
      //  private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
     //   private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
      //  private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
      //  private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn8;
        private System.Windows.Forms.Button ValidateFKbutton;
        private System.Windows.Forms.TextBox viewIDTextBox;
        private System.Windows.Forms.BindingSource primaryKeysBindingSource;
        //private System.Windows.Forms.DataGridViewCheckBoxColumn foundValueDataGridViewCheckBoxColumn;
        //private System.Windows.Forms.DataGridViewTextBoxColumn statusdescriptionDataGridViewTextBoxColumn;
        //private System.Windows.Forms.DataGridViewCheckBoxColumn createdDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn parentEntityIDDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn parentEntityColumnIDDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn entityColumnIDDataGridViewTextBoxColumn;
        private System.Windows.Forms.Button SaveEntitybutton;
        private System.Windows.Forms.DataGridView CustomQueryDatadataGridView;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Button ValidateFieldsbutton;
        private System.Windows.Forms.DataGridViewTextBoxColumn idDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn fieldnameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn fieldtypeDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn size1DataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn size2DataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn fieldCategoryDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn isAutoIncrementDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn allowDBNullDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn isCheckDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn isUniqueDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn isKeyDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn fieldIndexDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn entityNameDataGridViewTextBoxColumn;
    }
}
