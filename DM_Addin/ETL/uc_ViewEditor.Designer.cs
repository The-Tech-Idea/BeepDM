
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.ETL
{
    partial class uc_ViewEditor
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
            System.Windows.Forms.Label viewIDLabel;
            System.Windows.Forms.Label viewNameLabel;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(uc_ViewEditor));
            System.Windows.Forms.Label label1;
            this.dataViewDataSourceBindingNavigator = new System.Windows.Forms.BindingNavigator(this.components);
            this.bindingNavigatorAddNewItem = new System.Windows.Forms.ToolStripButton();
            this.dataViewDataSourceBindingSource = new System.Windows.Forms.BindingSource(this.components);
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
            this.dataViewDataSourceBindingNavigatorSaveItem = new System.Windows.Forms.ToolStripButton();
            this.viewIDTextBox = new System.Windows.Forms.TextBox();
            this.viewNameTextBox = new System.Windows.Forms.TextBox();
            this.entitiesBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.entitiesDataGridView = new System.Windows.Forms.DataGridView();
            this.dataConnectionsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.GridEntityName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.EntityDataSourceID = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.ViewtypeComboBox = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataSourcesBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.NewDatasourcecomboBox1 = new System.Windows.Forms.ComboBox();
            this.ChangeDatasourceButton = new System.Windows.Forms.Button();
            viewIDLabel = new System.Windows.Forms.Label();
            viewNameLabel = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataViewDataSourceBindingNavigator)).BeginInit();
            this.dataViewDataSourceBindingNavigator.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataViewDataSourceBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.entitiesBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.entitiesDataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataConnectionsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataSourcesBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // viewIDLabel
            // 
            viewIDLabel.AutoSize = true;
            viewIDLabel.Location = new System.Drawing.Point(149, 72);
            viewIDLabel.Name = "viewIDLabel";
            viewIDLabel.Size = new System.Drawing.Size(47, 13);
            viewIDLabel.TabIndex = 1;
            viewIDLabel.Text = "View ID:";
            // 
            // viewNameLabel
            // 
            viewNameLabel.AutoSize = true;
            viewNameLabel.Location = new System.Drawing.Point(132, 98);
            viewNameLabel.Name = "viewNameLabel";
            viewNameLabel.Size = new System.Drawing.Size(64, 13);
            viewNameLabel.TabIndex = 3;
            viewNameLabel.Text = "View Name:";
            // 
            // dataViewDataSourceBindingNavigator
            // 
            this.dataViewDataSourceBindingNavigator.AddNewItem = this.bindingNavigatorAddNewItem;
            this.dataViewDataSourceBindingNavigator.BindingSource = this.dataViewDataSourceBindingSource;
            this.dataViewDataSourceBindingNavigator.CountItem = this.bindingNavigatorCountItem;
            this.dataViewDataSourceBindingNavigator.DeleteItem = this.bindingNavigatorDeleteItem;
            this.dataViewDataSourceBindingNavigator.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
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
            this.dataViewDataSourceBindingNavigatorSaveItem});
            this.dataViewDataSourceBindingNavigator.Location = new System.Drawing.Point(0, 0);
            this.dataViewDataSourceBindingNavigator.MoveFirstItem = this.bindingNavigatorMoveFirstItem;
            this.dataViewDataSourceBindingNavigator.MoveLastItem = this.bindingNavigatorMoveLastItem;
            this.dataViewDataSourceBindingNavigator.MoveNextItem = this.bindingNavigatorMoveNextItem;
            this.dataViewDataSourceBindingNavigator.MovePreviousItem = this.bindingNavigatorMovePreviousItem;
            this.dataViewDataSourceBindingNavigator.Name = "dataViewDataSourceBindingNavigator";
            this.dataViewDataSourceBindingNavigator.PositionItem = this.bindingNavigatorPositionItem;
            this.dataViewDataSourceBindingNavigator.Size = new System.Drawing.Size(813, 25);
            this.dataViewDataSourceBindingNavigator.TabIndex = 0;
            this.dataViewDataSourceBindingNavigator.Text = "bindingNavigator1";
            // 
            // bindingNavigatorAddNewItem
            // 
            this.bindingNavigatorAddNewItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorAddNewItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorAddNewItem.Image")));
            this.bindingNavigatorAddNewItem.Name = "bindingNavigatorAddNewItem";
            this.bindingNavigatorAddNewItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorAddNewItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorAddNewItem.Text = "Add new";
            // 
            // dataViewDataSourceBindingSource
            // 
            this.dataViewDataSourceBindingSource.AllowNew = true;
            this.dataViewDataSourceBindingSource.DataSource = typeof(TheTechIdea.Beep.DataBase.DMDataView);
            // 
            // bindingNavigatorCountItem
            // 
            this.bindingNavigatorCountItem.Name = "bindingNavigatorCountItem";
            this.bindingNavigatorCountItem.Size = new System.Drawing.Size(35, 22);
            this.bindingNavigatorCountItem.Text = "of {0}";
            this.bindingNavigatorCountItem.ToolTipText = "Total number of items";
            // 
            // bindingNavigatorDeleteItem
            // 
            this.bindingNavigatorDeleteItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorDeleteItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorDeleteItem.Image")));
            this.bindingNavigatorDeleteItem.Name = "bindingNavigatorDeleteItem";
            this.bindingNavigatorDeleteItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorDeleteItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorDeleteItem.Text = "Delete";
            // 
            // bindingNavigatorMoveFirstItem
            // 
            this.bindingNavigatorMoveFirstItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorMoveFirstItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorMoveFirstItem.Image")));
            this.bindingNavigatorMoveFirstItem.Name = "bindingNavigatorMoveFirstItem";
            this.bindingNavigatorMoveFirstItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorMoveFirstItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorMoveFirstItem.Text = "Move first";
            // 
            // bindingNavigatorMovePreviousItem
            // 
            this.bindingNavigatorMovePreviousItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorMovePreviousItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorMovePreviousItem.Image")));
            this.bindingNavigatorMovePreviousItem.Name = "bindingNavigatorMovePreviousItem";
            this.bindingNavigatorMovePreviousItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorMovePreviousItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorMovePreviousItem.Text = "Move previous";
            // 
            // bindingNavigatorSeparator
            // 
            this.bindingNavigatorSeparator.Name = "bindingNavigatorSeparator";
            this.bindingNavigatorSeparator.Size = new System.Drawing.Size(6, 25);
            // 
            // bindingNavigatorPositionItem
            // 
            this.bindingNavigatorPositionItem.AccessibleName = "Position";
            this.bindingNavigatorPositionItem.AutoSize = false;
            this.bindingNavigatorPositionItem.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.bindingNavigatorPositionItem.Name = "bindingNavigatorPositionItem";
            this.bindingNavigatorPositionItem.Size = new System.Drawing.Size(50, 23);
            this.bindingNavigatorPositionItem.Text = "0";
            this.bindingNavigatorPositionItem.ToolTipText = "Current position";
            // 
            // bindingNavigatorSeparator1
            // 
            this.bindingNavigatorSeparator1.Name = "bindingNavigatorSeparator1";
            this.bindingNavigatorSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // bindingNavigatorMoveNextItem
            // 
            this.bindingNavigatorMoveNextItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorMoveNextItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorMoveNextItem.Image")));
            this.bindingNavigatorMoveNextItem.Name = "bindingNavigatorMoveNextItem";
            this.bindingNavigatorMoveNextItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorMoveNextItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorMoveNextItem.Text = "Move next";
            // 
            // bindingNavigatorMoveLastItem
            // 
            this.bindingNavigatorMoveLastItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorMoveLastItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorMoveLastItem.Image")));
            this.bindingNavigatorMoveLastItem.Name = "bindingNavigatorMoveLastItem";
            this.bindingNavigatorMoveLastItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorMoveLastItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorMoveLastItem.Text = "Move last";
            // 
            // bindingNavigatorSeparator2
            // 
            this.bindingNavigatorSeparator2.Name = "bindingNavigatorSeparator2";
            this.bindingNavigatorSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // dataViewDataSourceBindingNavigatorSaveItem
            // 
            this.dataViewDataSourceBindingNavigatorSaveItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.dataViewDataSourceBindingNavigatorSaveItem.Image = ((System.Drawing.Image)(resources.GetObject("dataViewDataSourceBindingNavigatorSaveItem.Image")));
            this.dataViewDataSourceBindingNavigatorSaveItem.Name = "dataViewDataSourceBindingNavigatorSaveItem";
            this.dataViewDataSourceBindingNavigatorSaveItem.Size = new System.Drawing.Size(23, 22);
            this.dataViewDataSourceBindingNavigatorSaveItem.Text = "Save Data";
            // 
            // viewIDTextBox
            // 
            this.viewIDTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.dataViewDataSourceBindingSource, "ViewID", true));
            this.viewIDTextBox.Location = new System.Drawing.Point(202, 69);
            this.viewIDTextBox.Name = "viewIDTextBox";
            this.viewIDTextBox.Size = new System.Drawing.Size(121, 20);
            this.viewIDTextBox.TabIndex = 2;
            // 
            // viewNameTextBox
            // 
            this.viewNameTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.dataViewDataSourceBindingSource, "ViewName", true));
            this.viewNameTextBox.Location = new System.Drawing.Point(202, 95);
            this.viewNameTextBox.Name = "viewNameTextBox";
            this.viewNameTextBox.Size = new System.Drawing.Size(320, 20);
            this.viewNameTextBox.TabIndex = 4;
            // 
            // entitiesBindingSource
            // 
            this.entitiesBindingSource.DataMember = "Entities";
            this.entitiesBindingSource.DataSource = this.dataViewDataSourceBindingSource;
            // 
            // entitiesDataGridView
            // 
            this.entitiesDataGridView.AllowUserToOrderColumns = true;
            this.entitiesDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.entitiesDataGridView.AutoGenerateColumns = false;
            this.entitiesDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.entitiesDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn1,
            this.GridEntityName,
            this.EntityDataSourceID,
            this.ViewtypeComboBox,
            this.dataGridViewTextBoxColumn2});
            this.entitiesDataGridView.DataSource = this.entitiesBindingSource;
            this.entitiesDataGridView.Location = new System.Drawing.Point(202, 148);
            this.entitiesDataGridView.Name = "entitiesDataGridView";
            this.entitiesDataGridView.Size = new System.Drawing.Size(597, 414);
            this.entitiesDataGridView.TabIndex = 11;
            // 
            // dataConnectionsBindingSource
            // 
            this.dataConnectionsBindingSource.DataSource = typeof(TheTechIdea.Util.ConnectionProperties);
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn1.DataPropertyName = "Id";
            this.dataGridViewTextBoxColumn1.HeaderText = "Id";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.Width = 41;
            // 
            // GridEntityName
            // 
            this.GridEntityName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.GridEntityName.DataPropertyName = "EntityName";
            this.GridEntityName.HeaderText = "EntityName";
            this.GridEntityName.Name = "GridEntityName";
            // 
            // EntityDataSourceID
            // 
            this.EntityDataSourceID.DataPropertyName = "DataSourceID";
            this.EntityDataSourceID.DataSource = this.dataConnectionsBindingSource;
            this.EntityDataSourceID.DisplayMember = "ConnectionName";
            this.EntityDataSourceID.HeaderText = "DataSourceID";
            this.EntityDataSourceID.Name = "EntityDataSourceID";
            this.EntityDataSourceID.ValueMember = "ConnectionName";
            // 
            // ViewtypeComboBox
            // 
            this.ViewtypeComboBox.DataPropertyName = "Viewtype";
            this.ViewtypeComboBox.HeaderText = "Viewtype";
            this.ViewtypeComboBox.Name = "ViewtypeComboBox";
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.dataGridViewTextBoxColumn2.DataPropertyName = "CustomBuildQuery";
            this.dataGridViewTextBoxColumn2.HeaderText = "CustomBuildQuery";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            // 
            // dataSourcesBindingSource
            // 
            this.dataSourcesBindingSource.DataSource = typeof(TheTechIdea.IDataSource);
            // 
            // NewDatasourcecomboBox1
            // 
            this.NewDatasourcecomboBox1.DataSource = this.dataConnectionsBindingSource;
            this.NewDatasourcecomboBox1.DisplayMember = "ConnectionName";
            this.NewDatasourcecomboBox1.FormattingEnabled = true;
            this.NewDatasourcecomboBox1.Location = new System.Drawing.Point(39, 148);
            this.NewDatasourcecomboBox1.Name = "NewDatasourcecomboBox1";
            this.NewDatasourcecomboBox1.Size = new System.Drawing.Size(121, 21);
            this.NewDatasourcecomboBox1.TabIndex = 12;
            this.NewDatasourcecomboBox1.ValueMember = "ConnectionName";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(67, 132);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(64, 13);
            label1.TabIndex = 13;
            label1.Text = "DataSource";
            // 
            // ChangeDatasourceButton
            // 
            this.ChangeDatasourceButton.Location = new System.Drawing.Point(62, 175);
            this.ChangeDatasourceButton.Name = "ChangeDatasourceButton";
            this.ChangeDatasourceButton.Size = new System.Drawing.Size(75, 23);
            this.ChangeDatasourceButton.TabIndex = 14;
            this.ChangeDatasourceButton.Text = "Change";
            this.ChangeDatasourceButton.UseVisualStyleBackColor = true;
            // 
            // uc_ViewEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ChangeDatasourceButton);
            this.Controls.Add(label1);
            this.Controls.Add(this.NewDatasourcecomboBox1);
            this.Controls.Add(this.entitiesDataGridView);
            this.Controls.Add(viewNameLabel);
            this.Controls.Add(this.viewNameTextBox);
            this.Controls.Add(viewIDLabel);
            this.Controls.Add(this.viewIDTextBox);
            this.Controls.Add(this.dataViewDataSourceBindingNavigator);
            this.Name = "uc_ViewEditor";
            this.Size = new System.Drawing.Size(813, 583);
            ((System.ComponentModel.ISupportInitialize)(this.dataViewDataSourceBindingNavigator)).EndInit();
            this.dataViewDataSourceBindingNavigator.ResumeLayout(false);
            this.dataViewDataSourceBindingNavigator.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataViewDataSourceBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.entitiesBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.entitiesDataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataConnectionsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataSourcesBindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.BindingNavigator dataViewDataSourceBindingNavigator;
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
        private System.Windows.Forms.ToolStripButton dataViewDataSourceBindingNavigatorSaveItem;
        private System.Windows.Forms.TextBox viewIDTextBox;
        private System.Windows.Forms.TextBox viewNameTextBox;
        private System.Windows.Forms.DataGridView entitiesDataGridView;
        private System.Windows.Forms.BindingSource dataSourcesBindingSource;
        public System.Windows.Forms.BindingSource dataViewDataSourceBindingSource;
        public System.Windows.Forms.BindingSource entitiesBindingSource;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn GridEntityName;
        private System.Windows.Forms.DataGridViewComboBoxColumn EntityDataSourceID;
        private System.Windows.Forms.BindingSource dataConnectionsBindingSource;
        private System.Windows.Forms.DataGridViewComboBoxColumn ViewtypeComboBox;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.ComboBox NewDatasourcecomboBox1;
        private System.Windows.Forms.Button ChangeDatasourceButton;
    }
}
