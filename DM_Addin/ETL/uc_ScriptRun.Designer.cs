
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
            this.progressBar1 = new TextProgressBar();
            this.RunScriptbutton = new System.Windows.Forms.Button();
            this.scriptDataGridView = new System.Windows.Forms.DataGridView();
            this.scriptTypeComboBox = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.sourcedatasourcename = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dataConnectionsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.destinationdatasourcename = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.Active = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.ddlDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.errormessageDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewComboBoxColumn1 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.trackingscriptDataGridView = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Messege = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.trackingscriptBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.dMEEditorBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.Log_panel = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.scriptBindingNavigator)).BeginInit();
            this.scriptBindingNavigator.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scriptBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.scriptDataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataConnectionsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackingscriptDataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackingscriptBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dMEEditorBindingSource)).BeginInit();
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
            this.scriptBindingSource.DataSource = typeof(TheTechIdea.DataManagment_Engine.Editor.LScript);
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
            this.label1.Location = new System.Drawing.Point(9, 37);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(148, 34);
            this.label1.TabIndex = 14;
            this.label1.Text = " Script Runner ";
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
            // progressBar1
            // 
            this.progressBar1.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.progressBar1.Location = new System.Drawing.Point(275, 743);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(734, 23);
            this.progressBar1.TabIndex = 11;
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
            this.scriptDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.scriptDataGridView.AutoGenerateColumns = false;
            this.scriptDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.scriptDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.scriptTypeComboBox,
            this.sourcedatasourcename,
            this.destinationdatasourcename,
            this.Active,
            this.ddlDataGridViewTextBoxColumn,
            this.errormessageDataGridViewTextBoxColumn});
            this.scriptDataGridView.DataSource = this.scriptBindingSource;
            this.scriptDataGridView.Location = new System.Drawing.Point(15, 103);
            this.scriptDataGridView.Name = "scriptDataGridView";
            this.scriptDataGridView.Size = new System.Drawing.Size(1255, 219);
            this.scriptDataGridView.TabIndex = 9;
            // 
            // scriptTypeComboBox
            // 
            this.scriptTypeComboBox.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.scriptTypeComboBox.DataPropertyName = "scriptType";
            this.scriptTypeComboBox.HeaderText = "scriptType";
            this.scriptTypeComboBox.Name = "scriptTypeComboBox";
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
            this.destinationdatasourcename.ValueMember = "ConnectionName";
            // 
            // Active
            // 
            this.Active.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.Active.DataPropertyName = "Active";
            this.Active.HeaderText = "Active";
            this.Active.Name = "Active";
            this.Active.Width = 43;
            // 
            // ddlDataGridViewTextBoxColumn
            // 
            this.ddlDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ddlDataGridViewTextBoxColumn.DataPropertyName = "ddl";
            this.ddlDataGridViewTextBoxColumn.HeaderText = "Script";
            this.ddlDataGridViewTextBoxColumn.Name = "ddlDataGridViewTextBoxColumn";
            // 
            // errormessageDataGridViewTextBoxColumn
            // 
            this.errormessageDataGridViewTextBoxColumn.DataPropertyName = "errormessage";
            this.errormessageDataGridViewTextBoxColumn.HeaderText = "errormessage";
            this.errormessageDataGridViewTextBoxColumn.Name = "errormessageDataGridViewTextBoxColumn";
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
            // trackingscriptDataGridView
            // 
            this.trackingscriptDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.trackingscriptDataGridView.AutoGenerateColumns = false;
            this.trackingscriptDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.trackingscriptDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn4,
            this.dataGridViewTextBoxColumn3,
            this.dataGridViewTextBoxColumn5,
            this.dataGridViewTextBoxColumn7,
            this.Messege});
            this.trackingscriptDataGridView.DataSource = this.trackingscriptBindingSource;
            this.trackingscriptDataGridView.Location = new System.Drawing.Point(117, 248);
            this.trackingscriptDataGridView.Name = "trackingscriptDataGridView";
            this.trackingscriptDataGridView.Size = new System.Drawing.Size(142, 62);
            this.trackingscriptDataGridView.TabIndex = 17;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.DataPropertyName = "currenrecordindex";
            this.dataGridViewTextBoxColumn4.HeaderText = "Index";
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "currentrecorddatasourcename";
            this.dataGridViewTextBoxColumn3.HeaderText = "DataSource";
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.DataPropertyName = "currenrecordentity";
            this.dataGridViewTextBoxColumn5.HeaderText = "Entity";
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            // 
            // dataGridViewTextBoxColumn7
            // 
            this.dataGridViewTextBoxColumn7.DataPropertyName = "scriptType";
            this.dataGridViewTextBoxColumn7.HeaderText = "Script Type";
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            // 
            // Messege
            // 
            this.Messege.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Messege.DataPropertyName = "errormessage";
            this.Messege.HeaderText = "Messege";
            this.Messege.Name = "Messege";
            // 
            // trackingscriptBindingSource
            // 
            this.trackingscriptBindingSource.DataSource = typeof(TheTechIdea.DataManagment_Engine.Editor.LScriptTracker);
            // 
            // dMEEditorBindingSource
            // 
            this.dMEEditorBindingSource.DataSource = typeof(TheTechIdea.DataManagment_Engine.DMEEditor);
            // 
            // Log_panel
            // 
            this.Log_panel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Log_panel.BackColor = System.Drawing.Color.Black;
            this.Log_panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Log_panel.ForeColor = System.Drawing.Color.Gold;
            this.Log_panel.Location = new System.Drawing.Point(15, 327);
            this.Log_panel.Margin = new System.Windows.Forms.Padding(2);
            this.Log_panel.Name = "Log_panel";
            this.Log_panel.ReadOnly = true;
            this.Log_panel.Size = new System.Drawing.Size(1255, 401);
            this.Log_panel.TabIndex = 18;
            this.Log_panel.Text = "";
            // 
            // uc_ScriptRun
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Log_panel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.StopButton);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.RunScriptbutton);
            this.Controls.Add(this.scriptDataGridView);
            this.Controls.Add(this.scriptBindingNavigator);
            this.Controls.Add(this.trackingscriptDataGridView);
            this.DoubleBuffered = true;
            this.Name = "uc_ScriptRun";
            this.Size = new System.Drawing.Size(1295, 804);
            ((System.ComponentModel.ISupportInitialize)(this.scriptBindingNavigator)).EndInit();
            this.scriptBindingNavigator.ResumeLayout(false);
            this.scriptBindingNavigator.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scriptBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.scriptDataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataConnectionsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackingscriptDataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackingscriptBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dMEEditorBindingSource)).EndInit();
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
        private System.Windows.Forms.DataGridViewTextBoxColumn scriptTypeComboBox;
        private System.Windows.Forms.DataGridViewComboBoxColumn sourcedatasourcename;
        private System.Windows.Forms.DataGridViewComboBoxColumn destinationdatasourcename;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Active;
        private System.Windows.Forms.DataGridViewTextBoxColumn entitynameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn ddlDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn errormessageDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.BindingSource dMEEditorBindingSource;
        private System.Windows.Forms.BindingSource trackingscriptBindingSource;
        private System.Windows.Forms.DataGridView trackingscriptDataGridView;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private System.Windows.Forms.DataGridViewTextBoxColumn Messege;
        private System.Windows.Forms.RichTextBox Log_panel;
    }
}
