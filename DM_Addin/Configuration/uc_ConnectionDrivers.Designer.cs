namespace TheTechIdea.Configuration
{
    partial class uc_ConnectionDrivers
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(uc_ConnectionDrivers));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.connectiondriversConfigBindingNavigator = new System.Windows.Forms.BindingNavigator(this.components);
            this.bindingNavigatorAddNewItem = new System.Windows.Forms.ToolStripButton();
            this.connectiondriversConfigBindingSource = new System.Windows.Forms.BindingSource(this.components);
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
            this.connectiondriversConfigBindingNavigatorSaveItem = new System.Windows.Forms.ToolStripButton();
            this.connectiondriversConfigDataGridView = new System.Windows.Forms.DataGridView();
            this.iconname = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.DatasourceCategoryComboBox = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.DatasourceTypeComboBox = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.classHandlerComboBox = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.CreateLocal = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.ADOType = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ConnectionString = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.connectiondriversConfigBindingNavigator)).BeginInit();
            this.connectiondriversConfigBindingNavigator.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.connectiondriversConfigBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.connectiondriversConfigDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // connectiondriversConfigBindingNavigator
            // 
            this.connectiondriversConfigBindingNavigator.AddNewItem = this.bindingNavigatorAddNewItem;
            this.connectiondriversConfigBindingNavigator.BindingSource = this.connectiondriversConfigBindingSource;
            this.connectiondriversConfigBindingNavigator.CountItem = this.bindingNavigatorCountItem;
            this.connectiondriversConfigBindingNavigator.DeleteItem = this.bindingNavigatorDeleteItem;
            this.connectiondriversConfigBindingNavigator.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.connectiondriversConfigBindingNavigator.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
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
            this.connectiondriversConfigBindingNavigatorSaveItem});
            this.connectiondriversConfigBindingNavigator.Location = new System.Drawing.Point(0, 0);
            this.connectiondriversConfigBindingNavigator.MoveFirstItem = this.bindingNavigatorMoveFirstItem;
            this.connectiondriversConfigBindingNavigator.MoveLastItem = this.bindingNavigatorMoveLastItem;
            this.connectiondriversConfigBindingNavigator.MoveNextItem = this.bindingNavigatorMoveNextItem;
            this.connectiondriversConfigBindingNavigator.MovePreviousItem = this.bindingNavigatorMovePreviousItem;
            this.connectiondriversConfigBindingNavigator.Name = "connectiondriversConfigBindingNavigator";
            this.connectiondriversConfigBindingNavigator.PositionItem = this.bindingNavigatorPositionItem;
            this.connectiondriversConfigBindingNavigator.Size = new System.Drawing.Size(1335, 31);
            this.connectiondriversConfigBindingNavigator.TabIndex = 0;
            this.connectiondriversConfigBindingNavigator.Text = "bindingNavigator1";
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
            // connectiondriversConfigBindingSource
            // 
            this.connectiondriversConfigBindingSource.DataSource = typeof(TheTechIdea.Util.ConnectionDriversConfig);
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
            this.bindingNavigatorPositionItem.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.bindingNavigatorPositionItem.Name = "bindingNavigatorPositionItem";
            this.bindingNavigatorPositionItem.Size = new System.Drawing.Size(35, 23);
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
            // connectiondriversConfigBindingNavigatorSaveItem
            // 
            this.connectiondriversConfigBindingNavigatorSaveItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.connectiondriversConfigBindingNavigatorSaveItem.Image = ((System.Drawing.Image)(resources.GetObject("connectiondriversConfigBindingNavigatorSaveItem.Image")));
            this.connectiondriversConfigBindingNavigatorSaveItem.Name = "connectiondriversConfigBindingNavigatorSaveItem";
            this.connectiondriversConfigBindingNavigatorSaveItem.Size = new System.Drawing.Size(28, 28);
            this.connectiondriversConfigBindingNavigatorSaveItem.Text = "Save Data";
            // 
            // connectiondriversConfigDataGridView
            // 
            this.connectiondriversConfigDataGridView.AutoGenerateColumns = false;
            this.connectiondriversConfigDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.connectiondriversConfigDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.connectiondriversConfigDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.connectiondriversConfigDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.iconname,
            this.DatasourceCategoryComboBox,
            this.DatasourceTypeComboBox,
            this.classHandlerComboBox,
            this.CreateLocal,
            this.ADOType,
            this.dataGridViewTextBoxColumn1,
            this.dataGridViewTextBoxColumn2,
            this.dataGridViewTextBoxColumn3,
            this.dataGridViewTextBoxColumn4,
            this.ConnectionString});
            this.connectiondriversConfigDataGridView.DataSource = this.connectiondriversConfigBindingSource;
            this.connectiondriversConfigDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.connectiondriversConfigDataGridView.Location = new System.Drawing.Point(0, 31);
            this.connectiondriversConfigDataGridView.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.connectiondriversConfigDataGridView.Name = "connectiondriversConfigDataGridView";
            this.connectiondriversConfigDataGridView.RowHeadersWidth = 62;
            this.connectiondriversConfigDataGridView.RowTemplate.Height = 28;
            this.connectiondriversConfigDataGridView.Size = new System.Drawing.Size(1335, 624);
            this.connectiondriversConfigDataGridView.TabIndex = 1;
            // 
            // iconname
            // 
            this.iconname.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.iconname.DataPropertyName = "iconname";
            this.iconname.HeaderText = "Icon";
            this.iconname.MinimumWidth = 8;
            this.iconname.Name = "iconname";
            this.iconname.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.iconname.Width = 44;
            // 
            // DatasourceCategoryComboBox
            // 
            this.DatasourceCategoryComboBox.DataPropertyName = "DatasourceCategory";
            this.DatasourceCategoryComboBox.HeaderText = "Category";
            this.DatasourceCategoryComboBox.Name = "DatasourceCategoryComboBox";
            // 
            // DatasourceTypeComboBox
            // 
            this.DatasourceTypeComboBox.DataPropertyName = "DatasourceType";
            this.DatasourceTypeComboBox.HeaderText = "Type";
            this.DatasourceTypeComboBox.Name = "DatasourceTypeComboBox";
            // 
            // classHandlerComboBox
            // 
            this.classHandlerComboBox.DataPropertyName = "classHandler";
            this.classHandlerComboBox.HeaderText = "Class Handler";
            this.classHandlerComboBox.MinimumWidth = 8;
            this.classHandlerComboBox.Name = "classHandlerComboBox";
            // 
            // CreateLocal
            // 
            this.CreateLocal.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.CreateLocal.DataPropertyName = "CreateLocal";
            this.CreateLocal.HeaderText = "Create Local";
            this.CreateLocal.MinimumWidth = 8;
            this.CreateLocal.Name = "CreateLocal";
            this.CreateLocal.Width = 106;
            // 
            // ADOType
            // 
            this.ADOType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.ADOType.DataPropertyName = "ADOType";
            this.ADOType.HeaderText = "ADO Type";
            this.ADOType.MinimumWidth = 8;
            this.ADOType.Name = "ADOType";
            this.ADOType.Width = 88;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.dataGridViewTextBoxColumn1.DataPropertyName = "PackageName";
            this.dataGridViewTextBoxColumn1.HeaderText = "Package Name";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 8;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.Width = 129;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.dataGridViewTextBoxColumn2.DataPropertyName = "DriverClass";
            this.dataGridViewTextBoxColumn2.HeaderText = "Driver Class";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 8;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.Width = 111;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.dataGridViewTextBoxColumn3.DataPropertyName = "version";
            this.dataGridViewTextBoxColumn3.HeaderText = "Version";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 8;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.Width = 88;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.dataGridViewTextBoxColumn4.DataPropertyName = "dllname";
            this.dataGridViewTextBoxColumn4.HeaderText = "DLL";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 8;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.Width = 62;
            // 
            // ConnectionString
            // 
            this.ConnectionString.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ConnectionString.DataPropertyName = "ConnectionString";
            this.ConnectionString.HeaderText = "Connection String";
            this.ConnectionString.MinimumWidth = 8;
            this.ConnectionString.Name = "ConnectionString";
            // 
            // uc_ConnectionDrivers
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.connectiondriversConfigDataGridView);
            this.Controls.Add(this.connectiondriversConfigBindingNavigator);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "uc_ConnectionDrivers";
            this.Size = new System.Drawing.Size(1335, 655);
            ((System.ComponentModel.ISupportInitialize)(this.connectiondriversConfigBindingNavigator)).EndInit();
            this.connectiondriversConfigBindingNavigator.ResumeLayout(false);
            this.connectiondriversConfigBindingNavigator.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.connectiondriversConfigBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.connectiondriversConfigDataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.BindingSource connectiondriversConfigBindingSource;
        private System.Windows.Forms.BindingNavigator connectiondriversConfigBindingNavigator;
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
        private System.Windows.Forms.ToolStripButton connectiondriversConfigBindingNavigatorSaveItem;
        private System.Windows.Forms.DataGridView connectiondriversConfigDataGridView;
        private System.Windows.Forms.DataGridViewComboBoxColumn iconname;
        private System.Windows.Forms.DataGridViewComboBoxColumn DatasourceCategoryComboBox;
        private System.Windows.Forms.DataGridViewComboBoxColumn DatasourceTypeComboBox;
        private System.Windows.Forms.DataGridViewComboBoxColumn classHandlerComboBox;
        private System.Windows.Forms.DataGridViewCheckBoxColumn CreateLocal;
        private System.Windows.Forms.DataGridViewCheckBoxColumn ADOType;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn ConnectionString;
    }
}
