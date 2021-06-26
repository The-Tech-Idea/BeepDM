
using WinFormSharedProject.Controls;

namespace TheTechIdea.ETL
{
    partial class uc_ScriptRun
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(uc_ScriptRun));
            this.scriptBindingNavigator = new System.Windows.Forms.BindingNavigator(this.components);
            this.bindingNavigatorAddNewItem = new System.Windows.Forms.ToolStripButton();
            this.scriptBindingSource = new System.Windows.Forms.BindingSource(this.components);
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
            this.scriptBindingNavigatorSaveItem = new System.Windows.Forms.ToolStripButton();
            this.label1 = new System.Windows.Forms.Label();
            this.StopButton = new System.Windows.Forms.Button();
            this.RunScriptbutton = new System.Windows.Forms.Button();
            this.scriptDataGridView = new System.Windows.Forms.DataGridView();
            this.scriptTypeComboBox = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.sourcedatasourcename = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dataConnectionsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.destinationdatasourcename = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.errormessageDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ddlDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewComboBoxColumn1 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DataCopyScripts = new System.Windows.Forms.DataGridView();
            this.activeDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.sourcedatasourcenameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.sourceentitynameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.destinationdatasourcenameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.destinationentitynameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.errormessageDataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.childScriptsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.TrackingdataGridView = new System.Windows.Forms.DataGridView();
            this.rundateDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.currenrecordindexDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.errormessageDataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.trackingBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.label2 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dMEEditorBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.progressBar1 = new WinFormSharedProject.Controls.TextProgressBar();
            this.ErrorsAllowdnumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.scriptBindingNavigator)).BeginInit();
            this.scriptBindingNavigator.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scriptBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.scriptDataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataConnectionsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DataCopyScripts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.childScriptsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TrackingdataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackingBindingSource)).BeginInit();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dMEEditorBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ErrorsAllowdnumericUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // scriptBindingNavigator
            // 
            this.scriptBindingNavigator.AddNewItem = this.bindingNavigatorAddNewItem;
            this.scriptBindingNavigator.BindingSource = this.scriptBindingSource;
            this.scriptBindingNavigator.CountItem = this.bindingNavigatorCountItem;
            this.scriptBindingNavigator.DeleteItem = this.bindingNavigatorDeleteItem;
            this.scriptBindingNavigator.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
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
            this.scriptBindingNavigatorSaveItem});
            this.scriptBindingNavigator.Location = new System.Drawing.Point(0, 0);
            this.scriptBindingNavigator.MoveFirstItem = this.bindingNavigatorMoveFirstItem;
            this.scriptBindingNavigator.MoveLastItem = this.bindingNavigatorMoveLastItem;
            this.scriptBindingNavigator.MoveNextItem = this.bindingNavigatorMoveNextItem;
            this.scriptBindingNavigator.MovePreviousItem = this.bindingNavigatorMovePreviousItem;
            this.scriptBindingNavigator.Name = "scriptBindingNavigator";
            this.scriptBindingNavigator.PositionItem = this.bindingNavigatorPositionItem;
            this.scriptBindingNavigator.Size = new System.Drawing.Size(1295, 25);
            this.scriptBindingNavigator.TabIndex = 1;
            this.scriptBindingNavigator.Text = "bindingNavigator1";
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
            // scriptBindingSource
            // 
            this.scriptBindingSource.DataSource = typeof(TheTechIdea.DataManagment_Engine.Editor.SyncEntity);
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
            // scriptBindingNavigatorSaveItem
            // 
            this.scriptBindingNavigatorSaveItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.scriptBindingNavigatorSaveItem.Image = ((System.Drawing.Image)(resources.GetObject("scriptBindingNavigatorSaveItem.Image")));
            this.scriptBindingNavigatorSaveItem.Name = "scriptBindingNavigatorSaveItem";
            this.scriptBindingNavigatorSaveItem.Size = new System.Drawing.Size(23, 22);
            this.scriptBindingNavigatorSaveItem.Text = "Save Data";
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Myanmar Text", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(587, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(138, 34);
            this.label1.TabIndex = 14;
            this.label1.Text = "ETL Data Run ";
            // 
            // StopButton
            // 
            this.StopButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.StopButton.Location = new System.Drawing.Point(1015, 743);
            this.StopButton.Name = "StopButton";
            this.StopButton.Size = new System.Drawing.Size(75, 23);
            this.StopButton.TabIndex = 12;
            this.StopButton.Text = "Stop";
            this.StopButton.UseVisualStyleBackColor = true;
            // 
            // RunScriptbutton
            // 
            this.RunScriptbutton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.RunScriptbutton.Location = new System.Drawing.Point(184, 743);
            this.RunScriptbutton.Name = "RunScriptbutton";
            this.RunScriptbutton.Size = new System.Drawing.Size(75, 23);
            this.RunScriptbutton.TabIndex = 10;
            this.RunScriptbutton.Text = "Run";
            this.RunScriptbutton.UseVisualStyleBackColor = true;
            // 
            // scriptDataGridView
            // 
            this.scriptDataGridView.AllowUserToDeleteRows = false;
            this.scriptDataGridView.AllowUserToOrderColumns = true;
            this.scriptDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.scriptDataGridView.AutoGenerateColumns = false;
            this.scriptDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.scriptDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.scriptTypeComboBox,
            this.sourcedatasourcename,
            this.destinationdatasourcename,
            this.errormessageDataGridViewTextBoxColumn,
            this.ddlDataGridViewTextBoxColumn});
            this.scriptDataGridView.DataSource = this.scriptBindingSource;
            this.scriptDataGridView.Location = new System.Drawing.Point(3, 34);
            this.scriptDataGridView.Name = "scriptDataGridView";
            this.scriptDataGridView.ReadOnly = true;
            this.scriptDataGridView.ShowCellErrors = false;
            this.scriptDataGridView.Size = new System.Drawing.Size(1260, 197);
            this.scriptDataGridView.TabIndex = 9;
            // 
            // scriptTypeComboBox
            // 
            this.scriptTypeComboBox.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.scriptTypeComboBox.DataPropertyName = "scriptType";
            this.scriptTypeComboBox.HeaderText = "scriptType";
            this.scriptTypeComboBox.Name = "scriptTypeComboBox";
            this.scriptTypeComboBox.ReadOnly = true;
            this.scriptTypeComboBox.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.scriptTypeComboBox.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.scriptTypeComboBox.Width = 62;
            // 
            // sourcedatasourcename
            // 
            this.sourcedatasourcename.DataPropertyName = "sourcedatasourcename";
            this.sourcedatasourcename.DataSource = this.dataConnectionsBindingSource;
            this.sourcedatasourcename.DisplayMember = "ConnectionName";
            this.sourcedatasourcename.HeaderText = "Source";
            this.sourcedatasourcename.Name = "sourcedatasourcename";
            this.sourcedatasourcename.ReadOnly = true;
            this.sourcedatasourcename.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.sourcedatasourcename.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.sourcedatasourcename.ValueMember = "ConnectionName";
            // 
            // dataConnectionsBindingSource
            // 
            this.dataConnectionsBindingSource.DataSource = typeof(TheTechIdea.Util.ConnectionProperties);
            // 
            // destinationdatasourcename
            // 
            this.destinationdatasourcename.DataPropertyName = "destinationdatasourcename";
            this.destinationdatasourcename.DataSource = this.dataConnectionsBindingSource;
            this.destinationdatasourcename.DisplayMember = "ConnectionName";
            this.destinationdatasourcename.HeaderText = "Destination";
            this.destinationdatasourcename.Name = "destinationdatasourcename";
            this.destinationdatasourcename.ReadOnly = true;
            this.destinationdatasourcename.ValueMember = "ConnectionName";
            // 
            // errormessageDataGridViewTextBoxColumn
            // 
            this.errormessageDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.errormessageDataGridViewTextBoxColumn.DataPropertyName = "errormessage";
            this.errormessageDataGridViewTextBoxColumn.HeaderText = "Messege";
            this.errormessageDataGridViewTextBoxColumn.Name = "errormessageDataGridViewTextBoxColumn";
            this.errormessageDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // ddlDataGridViewTextBoxColumn
            // 
            this.ddlDataGridViewTextBoxColumn.DataPropertyName = "ddl";
            this.ddlDataGridViewTextBoxColumn.HeaderText = "Script";
            this.ddlDataGridViewTextBoxColumn.Name = "ddlDataGridViewTextBoxColumn";
            this.ddlDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // dataGridViewComboBoxColumn1
            // 
            this.dataGridViewComboBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.dataGridViewComboBoxColumn1.DataPropertyName = "scriptType";
            this.dataGridViewComboBoxColumn1.HeaderText = "scriptType";
            this.dataGridViewComboBoxColumn1.Name = "dataGridViewComboBoxColumn1";
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.dataGridViewTextBoxColumn1.DataPropertyName = "scriptType";
            this.dataGridViewTextBoxColumn1.HeaderText = "scriptType";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewTextBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // DataCopyScripts
            // 
            this.DataCopyScripts.AllowUserToDeleteRows = false;
            this.DataCopyScripts.AllowUserToOrderColumns = true;
            this.DataCopyScripts.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DataCopyScripts.AutoGenerateColumns = false;
            this.DataCopyScripts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DataCopyScripts.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.activeDataGridViewCheckBoxColumn,
            this.sourcedatasourcenameDataGridViewTextBoxColumn,
            this.sourceentitynameDataGridViewTextBoxColumn,
            this.destinationdatasourcenameDataGridViewTextBoxColumn,
            this.destinationentitynameDataGridViewTextBoxColumn,
            this.errormessageDataGridViewTextBoxColumn1});
            this.DataCopyScripts.DataSource = this.childScriptsBindingSource;
            this.DataCopyScripts.Location = new System.Drawing.Point(3, 28);
            this.DataCopyScripts.Name = "DataCopyScripts";
            this.DataCopyScripts.ReadOnly = true;
            this.DataCopyScripts.ShowCellErrors = false;
            this.DataCopyScripts.Size = new System.Drawing.Size(1260, 141);
            this.DataCopyScripts.TabIndex = 18;
            // 
            // activeDataGridViewCheckBoxColumn
            // 
            this.activeDataGridViewCheckBoxColumn.DataPropertyName = "Active";
            this.activeDataGridViewCheckBoxColumn.HeaderText = "Active";
            this.activeDataGridViewCheckBoxColumn.Name = "activeDataGridViewCheckBoxColumn";
            this.activeDataGridViewCheckBoxColumn.ReadOnly = true;
            // 
            // sourcedatasourcenameDataGridViewTextBoxColumn
            // 
            this.sourcedatasourcenameDataGridViewTextBoxColumn.DataPropertyName = "sourcedatasourcename";
            this.sourcedatasourcenameDataGridViewTextBoxColumn.HeaderText = "Source";
            this.sourcedatasourcenameDataGridViewTextBoxColumn.Name = "sourcedatasourcenameDataGridViewTextBoxColumn";
            this.sourcedatasourcenameDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // sourceentitynameDataGridViewTextBoxColumn
            // 
            this.sourceentitynameDataGridViewTextBoxColumn.DataPropertyName = "sourceentityname";
            this.sourceentitynameDataGridViewTextBoxColumn.HeaderText = "Entity";
            this.sourceentitynameDataGridViewTextBoxColumn.Name = "sourceentitynameDataGridViewTextBoxColumn";
            this.sourceentitynameDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // destinationdatasourcenameDataGridViewTextBoxColumn
            // 
            this.destinationdatasourcenameDataGridViewTextBoxColumn.DataPropertyName = "destinationdatasourcename";
            this.destinationdatasourcenameDataGridViewTextBoxColumn.HeaderText = "Dest.";
            this.destinationdatasourcenameDataGridViewTextBoxColumn.Name = "destinationdatasourcenameDataGridViewTextBoxColumn";
            this.destinationdatasourcenameDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // destinationentitynameDataGridViewTextBoxColumn
            // 
            this.destinationentitynameDataGridViewTextBoxColumn.DataPropertyName = "destinationentityname";
            this.destinationentitynameDataGridViewTextBoxColumn.HeaderText = "Entity";
            this.destinationentitynameDataGridViewTextBoxColumn.Name = "destinationentitynameDataGridViewTextBoxColumn";
            this.destinationentitynameDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // errormessageDataGridViewTextBoxColumn1
            // 
            this.errormessageDataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.errormessageDataGridViewTextBoxColumn1.DataPropertyName = "errormessage";
            this.errormessageDataGridViewTextBoxColumn1.HeaderText = "Messege";
            this.errormessageDataGridViewTextBoxColumn1.Name = "errormessageDataGridViewTextBoxColumn1";
            this.errormessageDataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // childScriptsBindingSource
            // 
            this.childScriptsBindingSource.DataMember = "CopyDataScripts";
            this.childScriptsBindingSource.DataSource = this.scriptBindingSource;
            // 
            // TrackingdataGridView
            // 
            this.TrackingdataGridView.AllowUserToDeleteRows = false;
            this.TrackingdataGridView.AllowUserToOrderColumns = true;
            this.TrackingdataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TrackingdataGridView.AutoGenerateColumns = false;
            this.TrackingdataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.TrackingdataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.rundateDataGridViewTextBoxColumn,
            this.currenrecordindexDataGridViewTextBoxColumn,
            this.errormessageDataGridViewTextBoxColumn2});
            this.TrackingdataGridView.DataSource = this.trackingBindingSource;
            this.TrackingdataGridView.Location = new System.Drawing.Point(8, 28);
            this.TrackingdataGridView.Name = "TrackingdataGridView";
            this.TrackingdataGridView.ReadOnly = true;
            this.TrackingdataGridView.ShowCellErrors = false;
            this.TrackingdataGridView.Size = new System.Drawing.Size(1251, 219);
            this.TrackingdataGridView.TabIndex = 19;
            // 
            // rundateDataGridViewTextBoxColumn
            // 
            this.rundateDataGridViewTextBoxColumn.DataPropertyName = "rundate";
            this.rundateDataGridViewTextBoxColumn.HeaderText = "rundate";
            this.rundateDataGridViewTextBoxColumn.Name = "rundateDataGridViewTextBoxColumn";
            this.rundateDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // currenrecordindexDataGridViewTextBoxColumn
            // 
            this.currenrecordindexDataGridViewTextBoxColumn.DataPropertyName = "currenrecordindex";
            this.currenrecordindexDataGridViewTextBoxColumn.HeaderText = "Record #";
            this.currenrecordindexDataGridViewTextBoxColumn.Name = "currenrecordindexDataGridViewTextBoxColumn";
            this.currenrecordindexDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // errormessageDataGridViewTextBoxColumn2
            // 
            this.errormessageDataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.errormessageDataGridViewTextBoxColumn2.DataPropertyName = "errormessage";
            this.errormessageDataGridViewTextBoxColumn2.HeaderText = "Messege";
            this.errormessageDataGridViewTextBoxColumn2.Name = "errormessageDataGridViewTextBoxColumn2";
            this.errormessageDataGridViewTextBoxColumn2.ReadOnly = true;
            // 
            // trackingBindingSource
            // 
            this.trackingBindingSource.DataMember = "Tracking";
            this.trackingBindingSource.DataSource = this.childScriptsBindingSource;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Myanmar Text", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(3, 6);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(124, 25);
            this.label2.TabIndex = 20;
            this.label2.Text = "Main Commands";
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.scriptDataGridView);
            this.panel1.Location = new System.Drawing.Point(15, 62);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1268, 236);
            this.panel1.TabIndex = 21;
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.ErrorsAllowdnumericUpDown);
            this.panel2.Controls.Add(this.label5);
            this.panel2.Controls.Add(this.label3);
            this.panel2.Controls.Add(this.DataCopyScripts);
            this.panel2.Location = new System.Drawing.Point(15, 304);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1268, 174);
            this.panel2.TabIndex = 22;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Myanmar Text", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(3, -1);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(98, 25);
            this.label3.TabIndex = 21;
            this.label3.Text = "Copy Entities";
            // 
            // panel3
            // 
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.label4);
            this.panel3.Controls.Add(this.TrackingdataGridView);
            this.panel3.Location = new System.Drawing.Point(15, 485);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(1264, 252);
            this.panel3.TabIndex = 23;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Myanmar Text", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(3, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(76, 25);
            this.label4.TabIndex = 22;
            this.label4.Text = "Messsege";
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.dataGridViewTextBoxColumn2.DataPropertyName = "scriptType";
            this.dataGridViewTextBoxColumn2.HeaderText = "scriptType";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewTextBoxColumn2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.dataGridViewTextBoxColumn6.DataPropertyName = "scriptType";
            this.dataGridViewTextBoxColumn6.HeaderText = "scriptType";
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewTextBoxColumn6.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // dataGridViewTextBoxColumn8
            // 
            this.dataGridViewTextBoxColumn8.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.dataGridViewTextBoxColumn8.DataPropertyName = "scriptType";
            this.dataGridViewTextBoxColumn8.HeaderText = "scriptType";
            this.dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            this.dataGridViewTextBoxColumn8.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewTextBoxColumn8.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // dMEEditorBindingSource
            // 
            this.dMEEditorBindingSource.DataSource = typeof(TheTechIdea.DataManagment_Engine.DMEEditor);
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.progressBar1.CustomText = "";
            this.progressBar1.Location = new System.Drawing.Point(275, 743);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.ProgressColor = System.Drawing.Color.LightGreen;
            this.progressBar1.Size = new System.Drawing.Size(734, 23);
            this.progressBar1.TabIndex = 11;
            this.progressBar1.TextColor = System.Drawing.Color.Black;
            this.progressBar1.TextFont = new System.Drawing.Font("Times New Roman", 11F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))));
            this.progressBar1.VisualMode = WinFormSharedProject.Controls.ProgressBarDisplayMode.CurrProgress;
            // 
            // ErrorsAllowdnumericUpDown
            // 
            this.ErrorsAllowdnumericUpDown.Location = new System.Drawing.Point(1139, 4);
            this.ErrorsAllowdnumericUpDown.Name = "ErrorsAllowdnumericUpDown";
            this.ErrorsAllowdnumericUpDown.Size = new System.Drawing.Size(120, 20);
            this.ErrorsAllowdnumericUpDown.TabIndex = 24;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Myanmar Text", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(994, 2);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(115, 25);
            this.label5.TabIndex = 21;
            this.label5.Text = "Errors Allowed:";
            // 
            // uc_ScriptRun
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.StopButton);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.RunScriptbutton);
            this.Controls.Add(this.scriptBindingNavigator);
            this.DoubleBuffered = true;
            this.Name = "uc_ScriptRun";
            this.Size = new System.Drawing.Size(1295, 804);
            ((System.ComponentModel.ISupportInitialize)(this.scriptBindingNavigator)).EndInit();
            this.scriptBindingNavigator.ResumeLayout(false);
            this.scriptBindingNavigator.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scriptBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.scriptDataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataConnectionsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DataCopyScripts)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.childScriptsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TrackingdataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackingBindingSource)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dMEEditorBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ErrorsAllowdnumericUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.BindingSource scriptBindingSource;
        private System.Windows.Forms.BindingNavigator scriptBindingNavigator;
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
        private System.Windows.Forms.ToolStripButton scriptBindingNavigatorSaveItem;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button StopButton;
        private TextProgressBar progressBar1;
        private System.Windows.Forms.Button RunScriptbutton;
        private System.Windows.Forms.DataGridView scriptDataGridView;
        private System.Windows.Forms.DataGridViewComboBoxColumn dataGridViewComboBoxColumn1;
        private System.Windows.Forms.BindingSource dataConnectionsBindingSource;
        private System.Windows.Forms.DataGridViewTextBoxColumn entitynameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.BindingSource dMEEditorBindingSource;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private System.Windows.Forms.DataGridView DataCopyScripts;
        private System.Windows.Forms.BindingSource childScriptsBindingSource;
        private System.Windows.Forms.DataGridViewTextBoxColumn trackingDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridView TrackingdataGridView;
        private System.Windows.Forms.BindingSource trackingBindingSource;
        private System.Windows.Forms.DataGridViewTextBoxColumn rundateDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn currentrecorddatasourcenameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn currenrecordindexDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn currenrecordentityDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn errormessageDataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewCheckBoxColumn activeDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn sourcedatasourcenameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn sourceentitynameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn destinationdatasourcenameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn destinationentitynameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn errormessageDataGridViewTextBoxColumn1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn8;
        private System.Windows.Forms.DataGridViewTextBoxColumn scriptTypeComboBox;
        private System.Windows.Forms.DataGridViewComboBoxColumn sourcedatasourcename;
        private System.Windows.Forms.DataGridViewComboBoxColumn destinationdatasourcename;
        private System.Windows.Forms.DataGridViewTextBoxColumn errormessageDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn ddlDataGridViewTextBoxColumn;
        private System.Windows.Forms.NumericUpDown ErrorsAllowdnumericUpDown;
        private System.Windows.Forms.Label label5;
    }
}
