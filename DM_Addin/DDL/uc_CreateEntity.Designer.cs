
namespace TheTechIdea.DDL
{
    partial class uc_CreateEntity
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
            System.Windows.Forms.Label label1;
            System.Windows.Forms.Label statusdescriptionLabel;
            System.Windows.Forms.Label nameLabel;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(uc_CreateEntity));
            this.entitiesBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.entitiesBindingNavigator = new System.Windows.Forms.BindingNavigator(this.components);
            this.bindingNavigatorAddNewItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorCountItem = new System.Windows.Forms.ToolStripLabel();
            this.bindingNavigatorDeleteItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorMoveFirstItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorMovePreviousItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.bindingNavigatorPositionItem = new System.Windows.Forms.ToolStripTextBox();
            this.bindingNavigatorSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.bindingNavigatorMoveNextItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorMoveLastItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.entitiesBindingNavigatorSaveItem = new System.Windows.Forms.ToolStripButton();
            this.fieldsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.databaseTypeComboBox = new System.Windows.Forms.ComboBox();
            this.CreateinDBbutton = new System.Windows.Forms.Button();
            this.SaveTableConfigbutton = new System.Windows.Forms.Button();
            this.NewTablebutton = new System.Windows.Forms.Button();
            this.fieldsDataGridView = new System.Windows.Forms.DataGridView();
            this.idDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.fieldnameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.fieldtypeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.size1DataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.size2DataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.fieldCategoryDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.isAutoIncrementDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.allowDBNullDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.isCheckDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.isUniqueDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.isKeyDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.statusdescriptionTextBox = new System.Windows.Forms.TextBox();
            this.nameTextBox = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            statusdescriptionLabel = new System.Windows.Forms.Label();
            nameLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.entitiesBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.entitiesBindingNavigator)).BeginInit();
            this.entitiesBindingNavigator.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fieldsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fieldsDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(544, 622);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(70, 15);
            label1.TabIndex = 25;
            label1.Text = "Data Source";
            // 
            // statusdescriptionLabel
            // 
            statusdescriptionLabel.AutoSize = true;
            statusdescriptionLabel.Location = new System.Drawing.Point(238, 133);
            statusdescriptionLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            statusdescriptionLabel.Name = "statusdescriptionLabel";
            statusdescriptionLabel.Size = new System.Drawing.Size(90, 15);
            statusdescriptionLabel.TabIndex = 18;
            statusdescriptionLabel.Text = "Creating Status:";
            // 
            // nameLabel
            // 
            nameLabel.AutoSize = true;
            nameLabel.Location = new System.Drawing.Point(257, 109);
            nameLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            nameLabel.Name = "nameLabel";
            nameLabel.Size = new System.Drawing.Size(72, 15);
            nameLabel.TabIndex = 16;
            nameLabel.Text = "Table Name:";
            // 
            // entitiesBindingSource
            // 
            this.entitiesBindingSource.DataSource = typeof(TheTechIdea.DataManagment_Engine.DataBase.EntityStructure);
            // 
            // entitiesBindingNavigator
            // 
            this.entitiesBindingNavigator.AddNewItem = this.bindingNavigatorAddNewItem;
            this.entitiesBindingNavigator.BindingSource = this.entitiesBindingSource;
            this.entitiesBindingNavigator.CountItem = this.bindingNavigatorCountItem;
            this.entitiesBindingNavigator.DeleteItem = this.bindingNavigatorDeleteItem;
            this.entitiesBindingNavigator.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.entitiesBindingNavigator.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.bindingNavigatorMoveFirstItem,
            this.bindingNavigatorMovePreviousItem,
            this.bindingNavigatorSeparator,
            this.bindingNavigatorPositionItem,
            this.bindingNavigatorCountItem,
            this.bindingNavigatorSeparator1,
            this.bindingNavigatorMoveNextItem,
            this.bindingNavigatorMoveLastItem,
            this.bindingNavigatorSeparator2,
            this.bindingNavigatorAddNewItem,
            this.bindingNavigatorDeleteItem,
            this.entitiesBindingNavigatorSaveItem});
            this.entitiesBindingNavigator.Location = new System.Drawing.Point(0, 0);
            this.entitiesBindingNavigator.MoveFirstItem = this.bindingNavigatorMoveFirstItem;
            this.entitiesBindingNavigator.MoveLastItem = this.bindingNavigatorMoveLastItem;
            this.entitiesBindingNavigator.MoveNextItem = this.bindingNavigatorMoveNextItem;
            this.entitiesBindingNavigator.MovePreviousItem = this.bindingNavigatorMovePreviousItem;
            this.entitiesBindingNavigator.Name = "entitiesBindingNavigator";
            this.entitiesBindingNavigator.PositionItem = this.bindingNavigatorPositionItem;
            this.entitiesBindingNavigator.Size = new System.Drawing.Size(1267, 31);
            this.entitiesBindingNavigator.TabIndex = 15;
            this.entitiesBindingNavigator.Text = "bindingNavigator1";
            // 
            // bindingNavigatorAddNewItem
            // 
            this.bindingNavigatorAddNewItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorAddNewItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorAddNewItem.Image")));
            this.bindingNavigatorAddNewItem.Name = "bindingNavigatorAddNewItem";
            this.bindingNavigatorAddNewItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorAddNewItem.Size = new System.Drawing.Size(28, 28);
            this.bindingNavigatorAddNewItem.Text = "Add new";
            // 
            // bindingNavigatorCountItem
            // 
            this.bindingNavigatorCountItem.Name = "bindingNavigatorCountItem";
            this.bindingNavigatorCountItem.Size = new System.Drawing.Size(35, 28);
            this.bindingNavigatorCountItem.Text = "of {0}";
            this.bindingNavigatorCountItem.ToolTipText = "Total number of items";
            // 
            // bindingNavigatorDeleteItem
            // 
            this.bindingNavigatorDeleteItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorDeleteItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorDeleteItem.Image")));
            this.bindingNavigatorDeleteItem.Name = "bindingNavigatorDeleteItem";
            this.bindingNavigatorDeleteItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorDeleteItem.Size = new System.Drawing.Size(28, 28);
            this.bindingNavigatorDeleteItem.Text = "Delete";
            // 
            // bindingNavigatorMoveFirstItem
            // 
            this.bindingNavigatorMoveFirstItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorMoveFirstItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorMoveFirstItem.Image")));
            this.bindingNavigatorMoveFirstItem.Name = "bindingNavigatorMoveFirstItem";
            this.bindingNavigatorMoveFirstItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorMoveFirstItem.Size = new System.Drawing.Size(28, 28);
            this.bindingNavigatorMoveFirstItem.Text = "Move first";
            // 
            // bindingNavigatorMovePreviousItem
            // 
            this.bindingNavigatorMovePreviousItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorMovePreviousItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorMovePreviousItem.Image")));
            this.bindingNavigatorMovePreviousItem.Name = "bindingNavigatorMovePreviousItem";
            this.bindingNavigatorMovePreviousItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorMovePreviousItem.Size = new System.Drawing.Size(28, 28);
            this.bindingNavigatorMovePreviousItem.Text = "Move previous";
            // 
            // bindingNavigatorSeparator
            // 
            this.bindingNavigatorSeparator.Name = "bindingNavigatorSeparator";
            this.bindingNavigatorSeparator.Size = new System.Drawing.Size(6, 31);
            // 
            // bindingNavigatorPositionItem
            // 
            this.bindingNavigatorPositionItem.AccessibleName = "Position";
            this.bindingNavigatorPositionItem.AutoSize = false;
            this.bindingNavigatorPositionItem.Name = "bindingNavigatorPositionItem";
            this.bindingNavigatorPositionItem.Size = new System.Drawing.Size(40, 23);
            this.bindingNavigatorPositionItem.Text = "0";
            this.bindingNavigatorPositionItem.ToolTipText = "Current position";
            // 
            // bindingNavigatorSeparator1
            // 
            this.bindingNavigatorSeparator1.Name = "bindingNavigatorSeparator1";
            this.bindingNavigatorSeparator1.Size = new System.Drawing.Size(6, 31);
            // 
            // bindingNavigatorMoveNextItem
            // 
            this.bindingNavigatorMoveNextItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorMoveNextItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorMoveNextItem.Image")));
            this.bindingNavigatorMoveNextItem.Name = "bindingNavigatorMoveNextItem";
            this.bindingNavigatorMoveNextItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorMoveNextItem.Size = new System.Drawing.Size(28, 28);
            this.bindingNavigatorMoveNextItem.Text = "Move next";
            // 
            // bindingNavigatorMoveLastItem
            // 
            this.bindingNavigatorMoveLastItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorMoveLastItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorMoveLastItem.Image")));
            this.bindingNavigatorMoveLastItem.Name = "bindingNavigatorMoveLastItem";
            this.bindingNavigatorMoveLastItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorMoveLastItem.Size = new System.Drawing.Size(28, 28);
            this.bindingNavigatorMoveLastItem.Text = "Move last";
            // 
            // bindingNavigatorSeparator2
            // 
            this.bindingNavigatorSeparator2.Name = "bindingNavigatorSeparator2";
            this.bindingNavigatorSeparator2.Size = new System.Drawing.Size(6, 31);
            // 
            // entitiesBindingNavigatorSaveItem
            // 
            this.entitiesBindingNavigatorSaveItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.entitiesBindingNavigatorSaveItem.Image = ((System.Drawing.Image)(resources.GetObject("entitiesBindingNavigatorSaveItem.Image")));
            this.entitiesBindingNavigatorSaveItem.Name = "entitiesBindingNavigatorSaveItem";
            this.entitiesBindingNavigatorSaveItem.Size = new System.Drawing.Size(28, 28);
            this.entitiesBindingNavigatorSaveItem.Text = "Save Data";
            // 
            // fieldsBindingSource
            // 
            this.fieldsBindingSource.DataMember = "Fields";
            this.fieldsBindingSource.DataSource = this.entitiesBindingSource;
            // 
            // databaseTypeComboBox
            // 
            this.databaseTypeComboBox.FormattingEnabled = true;
            this.databaseTypeComboBox.Location = new System.Drawing.Point(627, 620);
            this.databaseTypeComboBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.databaseTypeComboBox.Name = "databaseTypeComboBox";
            this.databaseTypeComboBox.Size = new System.Drawing.Size(93, 23);
            this.databaseTypeComboBox.TabIndex = 24;
            // 
            // CreateinDBbutton
            // 
            this.CreateinDBbutton.Location = new System.Drawing.Point(723, 614);
            this.CreateinDBbutton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.CreateinDBbutton.Name = "CreateinDBbutton";
            this.CreateinDBbutton.Size = new System.Drawing.Size(92, 30);
            this.CreateinDBbutton.TabIndex = 23;
            this.CreateinDBbutton.Text = "Create in DB";
            this.CreateinDBbutton.UseVisualStyleBackColor = true;
            // 
            // SaveTableConfigbutton
            // 
            this.SaveTableConfigbutton.Location = new System.Drawing.Point(94, 282);
            this.SaveTableConfigbutton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.SaveTableConfigbutton.Name = "SaveTableConfigbutton";
            this.SaveTableConfigbutton.Size = new System.Drawing.Size(92, 30);
            this.SaveTableConfigbutton.TabIndex = 22;
            this.SaveTableConfigbutton.Text = "Save";
            this.SaveTableConfigbutton.UseVisualStyleBackColor = true;
            // 
            // NewTablebutton
            // 
            this.NewTablebutton.Location = new System.Drawing.Point(94, 243);
            this.NewTablebutton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.NewTablebutton.Name = "NewTablebutton";
            this.NewTablebutton.Size = new System.Drawing.Size(92, 30);
            this.NewTablebutton.TabIndex = 21;
            this.NewTablebutton.Text = "New";
            this.NewTablebutton.UseVisualStyleBackColor = true;
            // 
            // fieldsDataGridView
            // 
            this.fieldsDataGridView.AllowUserToAddRows = false;
            this.fieldsDataGridView.AutoGenerateColumns = false;
            this.fieldsDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.fieldsDataGridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCells;
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
            this.isKeyDataGridViewCheckBoxColumn});
            this.fieldsDataGridView.DataSource = this.fieldsBindingSource;
            this.fieldsDataGridView.Location = new System.Drawing.Point(201, 164);
            this.fieldsDataGridView.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.fieldsDataGridView.Name = "fieldsDataGridView";
            this.fieldsDataGridView.RowHeadersWidth = 62;
            this.fieldsDataGridView.RowTemplate.Height = 28;
            this.fieldsDataGridView.Size = new System.Drawing.Size(971, 445);
            this.fieldsDataGridView.TabIndex = 20;
            // 
            // idDataGridViewTextBoxColumn
            // 
            this.idDataGridViewTextBoxColumn.DataPropertyName = "id";
            this.idDataGridViewTextBoxColumn.HeaderText = "id";
            this.idDataGridViewTextBoxColumn.MinimumWidth = 8;
            this.idDataGridViewTextBoxColumn.Name = "idDataGridViewTextBoxColumn";
            this.idDataGridViewTextBoxColumn.Visible = false;
            // 
            // fieldnameDataGridViewTextBoxColumn
            // 
            this.fieldnameDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.fieldnameDataGridViewTextBoxColumn.DataPropertyName = "fieldname";
            this.fieldnameDataGridViewTextBoxColumn.HeaderText = "fieldname";
            this.fieldnameDataGridViewTextBoxColumn.MinimumWidth = 8;
            this.fieldnameDataGridViewTextBoxColumn.Name = "fieldnameDataGridViewTextBoxColumn";
            this.fieldnameDataGridViewTextBoxColumn.Width = 85;
            // 
            // fieldtypeDataGridViewTextBoxColumn
            // 
            this.fieldtypeDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.fieldtypeDataGridViewTextBoxColumn.DataPropertyName = "fieldtype";
            this.fieldtypeDataGridViewTextBoxColumn.HeaderText = "fieldtype";
            this.fieldtypeDataGridViewTextBoxColumn.MinimumWidth = 8;
            this.fieldtypeDataGridViewTextBoxColumn.Name = "fieldtypeDataGridViewTextBoxColumn";
            this.fieldtypeDataGridViewTextBoxColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.fieldtypeDataGridViewTextBoxColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.fieldtypeDataGridViewTextBoxColumn.Width = 78;
            // 
            // size1DataGridViewTextBoxColumn
            // 
            this.size1DataGridViewTextBoxColumn.DataPropertyName = "Size1";
            this.size1DataGridViewTextBoxColumn.HeaderText = "Size1";
            this.size1DataGridViewTextBoxColumn.MinimumWidth = 8;
            this.size1DataGridViewTextBoxColumn.Name = "size1DataGridViewTextBoxColumn";
            // 
            // size2DataGridViewTextBoxColumn
            // 
            this.size2DataGridViewTextBoxColumn.DataPropertyName = "Size2";
            this.size2DataGridViewTextBoxColumn.HeaderText = "Size2";
            this.size2DataGridViewTextBoxColumn.MinimumWidth = 8;
            this.size2DataGridViewTextBoxColumn.Name = "size2DataGridViewTextBoxColumn";
            // 
            // fieldCategoryDataGridViewTextBoxColumn
            // 
            this.fieldCategoryDataGridViewTextBoxColumn.DataPropertyName = "fieldCategory";
            this.fieldCategoryDataGridViewTextBoxColumn.HeaderText = "fieldCategory";
            this.fieldCategoryDataGridViewTextBoxColumn.MinimumWidth = 8;
            this.fieldCategoryDataGridViewTextBoxColumn.Name = "fieldCategoryDataGridViewTextBoxColumn";
            // 
            // isAutoIncrementDataGridViewCheckBoxColumn
            // 
            this.isAutoIncrementDataGridViewCheckBoxColumn.DataPropertyName = "IsAutoIncrement";
            this.isAutoIncrementDataGridViewCheckBoxColumn.HeaderText = "IsAutoIncrement";
            this.isAutoIncrementDataGridViewCheckBoxColumn.MinimumWidth = 8;
            this.isAutoIncrementDataGridViewCheckBoxColumn.Name = "isAutoIncrementDataGridViewCheckBoxColumn";
            // 
            // allowDBNullDataGridViewCheckBoxColumn
            // 
            this.allowDBNullDataGridViewCheckBoxColumn.DataPropertyName = "AllowDBNull";
            this.allowDBNullDataGridViewCheckBoxColumn.HeaderText = "AllowDBNull";
            this.allowDBNullDataGridViewCheckBoxColumn.MinimumWidth = 8;
            this.allowDBNullDataGridViewCheckBoxColumn.Name = "allowDBNullDataGridViewCheckBoxColumn";
            // 
            // isCheckDataGridViewCheckBoxColumn
            // 
            this.isCheckDataGridViewCheckBoxColumn.DataPropertyName = "IsCheck";
            this.isCheckDataGridViewCheckBoxColumn.HeaderText = "IsCheck";
            this.isCheckDataGridViewCheckBoxColumn.MinimumWidth = 8;
            this.isCheckDataGridViewCheckBoxColumn.Name = "isCheckDataGridViewCheckBoxColumn";
            // 
            // isUniqueDataGridViewCheckBoxColumn
            // 
            this.isUniqueDataGridViewCheckBoxColumn.DataPropertyName = "IsUnique";
            this.isUniqueDataGridViewCheckBoxColumn.HeaderText = "IsUnique";
            this.isUniqueDataGridViewCheckBoxColumn.MinimumWidth = 8;
            this.isUniqueDataGridViewCheckBoxColumn.Name = "isUniqueDataGridViewCheckBoxColumn";
            // 
            // isKeyDataGridViewCheckBoxColumn
            // 
            this.isKeyDataGridViewCheckBoxColumn.DataPropertyName = "IsKey";
            this.isKeyDataGridViewCheckBoxColumn.HeaderText = "IsKey";
            this.isKeyDataGridViewCheckBoxColumn.MinimumWidth = 8;
            this.isKeyDataGridViewCheckBoxColumn.Name = "isKeyDataGridViewCheckBoxColumn";
            // 
            // statusdescriptionTextBox
            // 
            this.statusdescriptionTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.entitiesBindingSource, "StatusDescription", true));
            this.statusdescriptionTextBox.Location = new System.Drawing.Point(338, 130);
            this.statusdescriptionTextBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.statusdescriptionTextBox.Name = "statusdescriptionTextBox";
            this.statusdescriptionTextBox.Size = new System.Drawing.Size(569, 23);
            this.statusdescriptionTextBox.TabIndex = 19;
            // 
            // nameTextBox
            // 
            this.nameTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.entitiesBindingSource, "EntityName", true));
            this.nameTextBox.Location = new System.Drawing.Point(338, 106);
            this.nameTextBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.Size = new System.Drawing.Size(287, 23);
            this.nameTextBox.TabIndex = 17;
            // 
            // uc_CreateEntity
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(label1);
            this.Controls.Add(this.databaseTypeComboBox);
            this.Controls.Add(this.CreateinDBbutton);
            this.Controls.Add(this.SaveTableConfigbutton);
            this.Controls.Add(this.NewTablebutton);
            this.Controls.Add(this.fieldsDataGridView);
            this.Controls.Add(statusdescriptionLabel);
            this.Controls.Add(this.statusdescriptionTextBox);
            this.Controls.Add(nameLabel);
            this.Controls.Add(this.nameTextBox);
            this.Controls.Add(this.entitiesBindingNavigator);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "uc_CreateEntity";
            this.Size = new System.Drawing.Size(1267, 751);
            ((System.ComponentModel.ISupportInitialize)(this.entitiesBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.entitiesBindingNavigator)).EndInit();
            this.entitiesBindingNavigator.ResumeLayout(false);
            this.entitiesBindingNavigator.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fieldsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fieldsDataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.BindingSource entitiesBindingSource;
        private System.Windows.Forms.BindingNavigator entitiesBindingNavigator;
        private System.Windows.Forms.ToolStripButton bindingNavigatorAddNewItem;
        private System.Windows.Forms.ToolStripLabel bindingNavigatorCountItem;
        private System.Windows.Forms.ToolStripButton bindingNavigatorDeleteItem;
        private System.Windows.Forms.ToolStripButton bindingNavigatorMoveFirstItem;
        private System.Windows.Forms.ToolStripButton bindingNavigatorMovePreviousItem;
        private System.Windows.Forms.ToolStripSeparator bindingNavigatorSeparator;
        private System.Windows.Forms.ToolStripTextBox bindingNavigatorPositionItem;
        private System.Windows.Forms.ToolStripSeparator bindingNavigatorSeparator1;
        private System.Windows.Forms.ToolStripButton bindingNavigatorMoveNextItem;
        private System.Windows.Forms.ToolStripButton bindingNavigatorMoveLastItem;
        private System.Windows.Forms.ToolStripSeparator bindingNavigatorSeparator2;
        private System.Windows.Forms.ToolStripButton entitiesBindingNavigatorSaveItem;
        private System.Windows.Forms.BindingSource fieldsBindingSource;
        private System.Windows.Forms.ComboBox databaseTypeComboBox;
        private System.Windows.Forms.Button CreateinDBbutton;
        private System.Windows.Forms.Button SaveTableConfigbutton;
        private System.Windows.Forms.Button NewTablebutton;
        private System.Windows.Forms.DataGridView fieldsDataGridView;
        private System.Windows.Forms.DataGridViewTextBoxColumn idDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn fieldnameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewComboBoxColumn fieldtypeDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn size1DataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn size2DataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn fieldCategoryDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn isAutoIncrementDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn allowDBNullDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn isCheckDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn isUniqueDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn isKeyDataGridViewCheckBoxColumn;
        private System.Windows.Forms.TextBox statusdescriptionTextBox;
        private System.Windows.Forms.TextBox nameTextBox;
    }
}
