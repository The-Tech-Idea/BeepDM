
namespace TheTechIdea.Configuration
{
    partial class uc_QueryConfig
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(uc_QueryConfig));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.queryListBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.queryListBindingNavigator = new System.Windows.Forms.BindingNavigator(this.components);
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
            this.queryListBindingNavigatorSaveItem = new System.Windows.Forms.ToolStripButton();
            this.DatabaseTypebindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.queryListDataGridView = new System.Windows.Forms.DataGridView();
            this.DatabasetypeComboBox = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.SQLTypeComboBox = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.queryListBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.queryListBindingNavigator)).BeginInit();
            this.queryListBindingNavigator.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DatabaseTypebindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.queryListDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // queryListBindingSource
            // 
            this.queryListBindingSource.DataSource = typeof(TheTechIdea.Util.QuerySqlRepo);
            // 
            // queryListBindingNavigator
            // 
            this.queryListBindingNavigator.AddNewItem = this.bindingNavigatorAddNewItem;
            this.queryListBindingNavigator.BindingSource = this.queryListBindingSource;
            this.queryListBindingNavigator.CountItem = this.bindingNavigatorCountItem;
            this.queryListBindingNavigator.DeleteItem = this.bindingNavigatorDeleteItem;
            this.queryListBindingNavigator.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.queryListBindingNavigator.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
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
            this.queryListBindingNavigatorSaveItem});
            this.queryListBindingNavigator.Location = new System.Drawing.Point(0, 0);
            this.queryListBindingNavigator.MoveFirstItem = this.bindingNavigatorMoveFirstItem;
            this.queryListBindingNavigator.MoveLastItem = this.bindingNavigatorMoveLastItem;
            this.queryListBindingNavigator.MoveNextItem = this.bindingNavigatorMoveNextItem;
            this.queryListBindingNavigator.MovePreviousItem = this.bindingNavigatorMovePreviousItem;
            this.queryListBindingNavigator.Name = "queryListBindingNavigator";
            this.queryListBindingNavigator.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            this.queryListBindingNavigator.PositionItem = this.bindingNavigatorPositionItem;
            this.queryListBindingNavigator.Size = new System.Drawing.Size(1196, 31);
            this.queryListBindingNavigator.TabIndex = 1;
            this.queryListBindingNavigator.Text = "bindingNavigator1";
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
            this.bindingNavigatorPositionItem.Size = new System.Drawing.Size(58, 23);
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
            // queryListBindingNavigatorSaveItem
            // 
            this.queryListBindingNavigatorSaveItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.queryListBindingNavigatorSaveItem.Image = ((System.Drawing.Image)(resources.GetObject("queryListBindingNavigatorSaveItem.Image")));
            this.queryListBindingNavigatorSaveItem.Name = "queryListBindingNavigatorSaveItem";
            this.queryListBindingNavigatorSaveItem.Size = new System.Drawing.Size(28, 28);
            this.queryListBindingNavigatorSaveItem.Text = "Save Data";
            // 
            // DatabaseTypebindingSource
            // 
            this.DatabaseTypebindingSource.DataSource = typeof(TheTechIdea.Util.QuerySqlRepo);
            // 
            // queryListDataGridView
            // 
            this.queryListDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.queryListDataGridView.AutoGenerateColumns = false;
            this.queryListDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.queryListDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.DatabasetypeComboBox,
            this.SQLTypeComboBox,
            this.dataGridViewTextBoxColumn3});
            this.queryListDataGridView.DataSource = this.queryListBindingSource;
            this.queryListDataGridView.Location = new System.Drawing.Point(30, 73);
            this.queryListDataGridView.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.queryListDataGridView.Name = "queryListDataGridView";
            this.queryListDataGridView.RowHeadersWidth = 62;
            this.queryListDataGridView.Size = new System.Drawing.Size(1134, 619);
            this.queryListDataGridView.TabIndex = 2;
            // 
            // DatabasetypeComboBox
            // 
            this.DatabasetypeComboBox.DataPropertyName = "DatabaseType";
            this.DatabasetypeComboBox.HeaderText = "DatabaseType";
            this.DatabasetypeComboBox.MinimumWidth = 8;
            this.DatabasetypeComboBox.Name = "DatabasetypeComboBox";
            this.DatabasetypeComboBox.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.DatabasetypeComboBox.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.DatabasetypeComboBox.Width = 150;
            // 
            // SQLTypeComboBox
            // 
            this.SQLTypeComboBox.DataPropertyName = "Sqltype";
            this.SQLTypeComboBox.HeaderText = "Sqltype";
            this.SQLTypeComboBox.MinimumWidth = 8;
            this.SQLTypeComboBox.Name = "SQLTypeComboBox";
            this.SQLTypeComboBox.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.SQLTypeComboBox.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.SQLTypeComboBox.Width = 150;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.dataGridViewTextBoxColumn3.DataPropertyName = "Sql";
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewTextBoxColumn3.DefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridViewTextBoxColumn3.HeaderText = "Sql";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 8;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            // 
            // uc_QueryConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.queryListDataGridView);
            this.Controls.Add(this.queryListBindingNavigator);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "uc_QueryConfig";
            this.Size = new System.Drawing.Size(1196, 732);
            ((System.ComponentModel.ISupportInitialize)(this.queryListBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.queryListBindingNavigator)).EndInit();
            this.queryListBindingNavigator.ResumeLayout(false);
            this.queryListBindingNavigator.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DatabaseTypebindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.queryListDataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.BindingSource queryListBindingSource;
        private System.Windows.Forms.BindingNavigator queryListBindingNavigator;
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
        private System.Windows.Forms.ToolStripButton queryListBindingNavigatorSaveItem;
        private System.Windows.Forms.BindingSource DatabaseTypebindingSource;
        private System.Windows.Forms.DataGridView queryListDataGridView;
        private System.Windows.Forms.DataGridViewComboBoxColumn DatabasetypeComboBox;
        private System.Windows.Forms.DataGridViewComboBoxColumn SQLTypeComboBox;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
    }
}
