namespace TheTechIdea.ETL
{
    partial class uc_txtfileManager
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
            System.Windows.Forms.Label label2;
            System.Windows.Forms.Label entityNameLabel;
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.fieldsDataGridView = new System.Windows.Forms.DataGridView();
            this.fieldsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.entitiesBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.SampledataGridView = new System.Windows.Forms.DataGridView();
            this.SaveConfigbutton = new System.Windows.Forms.Button();
            this.LoadSampleDatabutton = new System.Windows.Forms.Button();
            this.entityNameTextBox = new System.Windows.Forms.TextBox();
            this.idDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.fieldname = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.fieldtype = new System.Windows.Forms.DataGridViewComboBoxColumn();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            entityNameLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.fieldsDataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fieldsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.entitiesBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SampledataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label1.Location = new System.Drawing.Point(36, 190);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(74, 24);
            label1.TabIndex = 7;
            label1.Text = "Fields";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label2.Location = new System.Drawing.Point(566, 187);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(146, 24);
            label2.TabIndex = 9;
            label2.Text = "Sample Data";
            // 
            // entityNameLabel
            // 
            entityNameLabel.AutoSize = true;
            entityNameLabel.Location = new System.Drawing.Point(285, 65);
            entityNameLabel.Name = "entityNameLabel";
            entityNameLabel.Size = new System.Drawing.Size(99, 20);
            entityNameLabel.TabIndex = 11;
            entityNameLabel.Text = "Entity Name:";
            // 
            // fieldsDataGridView
            // 
            this.fieldsDataGridView.AllowUserToAddRows = false;
            this.fieldsDataGridView.AllowUserToDeleteRows = false;
            this.fieldsDataGridView.AutoGenerateColumns = false;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("MS PGothic", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.fieldsDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.fieldsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.fieldsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.fieldname,
            this.fieldtype});
            this.fieldsDataGridView.DataSource = this.fieldsBindingSource;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("MS PGothic", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.fieldsDataGridView.DefaultCellStyle = dataGridViewCellStyle2;
            this.fieldsDataGridView.Location = new System.Drawing.Point(40, 217);
            this.fieldsDataGridView.Name = "fieldsDataGridView";
            this.fieldsDataGridView.RowHeadersVisible = false;
            this.fieldsDataGridView.RowHeadersWidth = 62;
            this.fieldsDataGridView.RowTemplate.Height = 28;
            this.fieldsDataGridView.Size = new System.Drawing.Size(497, 723);
            this.fieldsDataGridView.TabIndex = 6;
            // 
            // fieldsBindingSource
            // 
            this.fieldsBindingSource.DataMember = "Fields";
            this.fieldsBindingSource.DataSource = this.entitiesBindingSource;
            // 
            // entitiesBindingSource
            // 
            this.entitiesBindingSource.DataSource = typeof(TheTechIdea.DataManagment_Engine.DataBase.EntityStructure);
            // 
            // SampledataGridView
            // 
            this.SampledataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SampledataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.SampledataGridView.Location = new System.Drawing.Point(570, 214);
            this.SampledataGridView.Name = "SampledataGridView";
            this.SampledataGridView.ReadOnly = true;
            this.SampledataGridView.RowHeadersWidth = 62;
            this.SampledataGridView.RowTemplate.Height = 28;
            this.SampledataGridView.Size = new System.Drawing.Size(894, 723);
            this.SampledataGridView.TabIndex = 8;
            // 
            // SaveConfigbutton
            // 
            this.SaveConfigbutton.Font = new System.Drawing.Font("Modern No. 20", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SaveConfigbutton.Location = new System.Drawing.Point(330, 165);
            this.SaveConfigbutton.Name = "SaveConfigbutton";
            this.SaveConfigbutton.Size = new System.Drawing.Size(207, 49);
            this.SaveConfigbutton.TabIndex = 10;
            this.SaveConfigbutton.Text = "Save Configuration";
            this.SaveConfigbutton.UseVisualStyleBackColor = true;
            // 
            // LoadSampleDatabutton
            // 
            this.LoadSampleDatabutton.Font = new System.Drawing.Font("Modern No. 20", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LoadSampleDatabutton.Location = new System.Drawing.Point(1257, 162);
            this.LoadSampleDatabutton.Name = "LoadSampleDatabutton";
            this.LoadSampleDatabutton.Size = new System.Drawing.Size(207, 49);
            this.LoadSampleDatabutton.TabIndex = 11;
            this.LoadSampleDatabutton.Text = "Load Sample";
            this.LoadSampleDatabutton.UseVisualStyleBackColor = true;
            // 
            // entityNameTextBox
            // 
            this.entityNameTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.entitiesBindingSource, "EntityName", true));
            this.entityNameTextBox.Location = new System.Drawing.Point(390, 62);
            this.entityNameTextBox.Name = "entityNameTextBox";
            this.entityNameTextBox.Size = new System.Drawing.Size(808, 26);
            this.entityNameTextBox.TabIndex = 12;
            // 
            // idDataGridViewTextBoxColumn
            // 
            this.idDataGridViewTextBoxColumn.DataPropertyName = "id";
            this.idDataGridViewTextBoxColumn.HeaderText = "id";
            this.idDataGridViewTextBoxColumn.MinimumWidth = 8;
            this.idDataGridViewTextBoxColumn.Name = "idDataGridViewTextBoxColumn";
            this.idDataGridViewTextBoxColumn.Width = 150;
            // 
            // fieldname
            // 
            this.fieldname.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.fieldname.DataPropertyName = "fieldname";
            this.fieldname.HeaderText = "fieldname";
            this.fieldname.MinimumWidth = 8;
            this.fieldname.Name = "fieldname";
            this.fieldname.Width = 133;
            // 
            // fieldtype
            // 
            this.fieldtype.DataPropertyName = "fieldtype";
            this.fieldtype.HeaderText = "Field Type";
            this.fieldtype.MinimumWidth = 8;
            this.fieldtype.Name = "fieldtype";
            this.fieldtype.Width = 150;
            // 
            // uc_txtfileManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(entityNameLabel);
            this.Controls.Add(this.entityNameTextBox);
            this.Controls.Add(this.LoadSampleDatabutton);
            this.Controls.Add(this.SaveConfigbutton);
            this.Controls.Add(label2);
            this.Controls.Add(this.SampledataGridView);
            this.Controls.Add(label1);
            this.Controls.Add(this.fieldsDataGridView);
            this.Name = "uc_txtfileManager";
            this.Size = new System.Drawing.Size(1514, 999);
            ((System.ComponentModel.ISupportInitialize)(this.fieldsDataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fieldsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.entitiesBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SampledataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.DataGridView fieldsDataGridView;
      //  private System.Windows.Forms.DataGridViewTextBoxColumn dataSourcetypeDataGridViewTextBoxColumn;
        private System.Windows.Forms.BindingSource fieldsBindingSource;
        private System.Windows.Forms.DataGridView SampledataGridView;
        private System.Windows.Forms.Button SaveConfigbutton;
      //  private System.Windows.Forms.DataGridViewTextBoxColumn fieldNameDataGridViewTextBoxColumn;
      //  private System.Windows.Forms.DataGridViewComboBoxColumn fieldTypeDataGridViewTextBoxColumn;
        private System.Windows.Forms.Button LoadSampleDatabutton;
        private System.Windows.Forms.BindingSource entitiesBindingSource;
        private System.Windows.Forms.TextBox entityNameTextBox;
        private System.Windows.Forms.DataGridViewTextBoxColumn idDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn fieldname;
        private System.Windows.Forms.DataGridViewComboBoxColumn fieldtype;
    }
}
