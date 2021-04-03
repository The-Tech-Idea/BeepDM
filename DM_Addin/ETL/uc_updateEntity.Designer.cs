
namespace TheTechIdea.ETL
{
    partial class uc_updateEntity
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
            System.Windows.Forms.Label idLabel;
            System.Windows.Forms.Label entityNameLabel;
            System.Windows.Forms.Label categoryLabel;
            System.Windows.Forms.Label databaseTypeLabel;
            System.Windows.Forms.Label dataSourceIDLabel;
            System.Windows.Forms.Label editableLabel;
            System.Windows.Forms.Label keyTokenLabel;
            System.Windows.Forms.Label parentIdLabel;
            System.Windows.Forms.Label showLabel;
            System.Windows.Forms.Label customBuildQueryLabel;
            this.entitiesBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.idTextBox = new System.Windows.Forms.TextBox();
            this.entityNameTextBox = new System.Windows.Forms.TextBox();
            this.categoryComboBox = new System.Windows.Forms.ComboBox();
            this.databaseTypeComboBox = new System.Windows.Forms.ComboBox();
            this.dataSourceIDComboBox = new System.Windows.Forms.ComboBox();
            this.editableCheckBox = new System.Windows.Forms.CheckBox();
            this.keyTokenTextBox = new System.Windows.Forms.TextBox();
            this.parentIdComboBox = new System.Windows.Forms.ComboBox();
            this.showCheckBox = new System.Windows.Forms.CheckBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.customBuildQueryTextBox = new System.Windows.Forms.TextBox();
            idLabel = new System.Windows.Forms.Label();
            entityNameLabel = new System.Windows.Forms.Label();
            categoryLabel = new System.Windows.Forms.Label();
            databaseTypeLabel = new System.Windows.Forms.Label();
            dataSourceIDLabel = new System.Windows.Forms.Label();
            editableLabel = new System.Windows.Forms.Label();
            keyTokenLabel = new System.Windows.Forms.Label();
            parentIdLabel = new System.Windows.Forms.Label();
            showLabel = new System.Windows.Forms.Label();
            customBuildQueryLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.entitiesBindingSource)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // entitiesBindingSource
            // 
            this.entitiesBindingSource.DataSource = typeof(TheTechIdea.DataManagment_Engine.DataBase.EntityStructure);
            // 
            // idLabel
            // 
            idLabel.AutoSize = true;
            idLabel.Location = new System.Drawing.Point(78, 25);
            idLabel.Name = "idLabel";
            idLabel.Size = new System.Drawing.Size(19, 13);
            idLabel.TabIndex = 0;
            idLabel.Text = "Id:";
            // 
            // idTextBox
            // 
            this.idTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.entitiesBindingSource, "Id", true));
            this.idTextBox.Location = new System.Drawing.Point(103, 22);
            this.idTextBox.Name = "idTextBox";
            this.idTextBox.Size = new System.Drawing.Size(100, 20);
            this.idTextBox.TabIndex = 1;
            // 
            // entityNameLabel
            // 
            entityNameLabel.AutoSize = true;
            entityNameLabel.Location = new System.Drawing.Point(30, 78);
            entityNameLabel.Name = "entityNameLabel";
            entityNameLabel.Size = new System.Drawing.Size(67, 13);
            entityNameLabel.TabIndex = 2;
            entityNameLabel.Text = "Entity Name:";
            // 
            // entityNameTextBox
            // 
            this.entityNameTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.entitiesBindingSource, "EntityName", true));
            this.entityNameTextBox.Location = new System.Drawing.Point(103, 75);
            this.entityNameTextBox.Name = "entityNameTextBox";
            this.entityNameTextBox.Size = new System.Drawing.Size(175, 20);
            this.entityNameTextBox.TabIndex = 3;
            // 
            // categoryLabel
            // 
            categoryLabel.AutoSize = true;
            categoryLabel.Location = new System.Drawing.Point(45, 104);
            categoryLabel.Name = "categoryLabel";
            categoryLabel.Size = new System.Drawing.Size(52, 13);
            categoryLabel.TabIndex = 4;
            categoryLabel.Text = "Category:";
            // 
            // categoryComboBox
            // 
            this.categoryComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.entitiesBindingSource, "Category", true));
            this.categoryComboBox.FormattingEnabled = true;
            this.categoryComboBox.Location = new System.Drawing.Point(103, 101);
            this.categoryComboBox.Name = "categoryComboBox";
            this.categoryComboBox.Size = new System.Drawing.Size(121, 21);
            this.categoryComboBox.TabIndex = 5;
            // 
            // databaseTypeLabel
            // 
            databaseTypeLabel.AutoSize = true;
            databaseTypeLabel.Location = new System.Drawing.Point(14, 131);
            databaseTypeLabel.Name = "databaseTypeLabel";
            databaseTypeLabel.Size = new System.Drawing.Size(83, 13);
            databaseTypeLabel.TabIndex = 6;
            databaseTypeLabel.Text = "Database Type:";
            // 
            // databaseTypeComboBox
            // 
            this.databaseTypeComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.entitiesBindingSource, "DatabaseType", true));
            this.databaseTypeComboBox.FormattingEnabled = true;
            this.databaseTypeComboBox.Location = new System.Drawing.Point(103, 128);
            this.databaseTypeComboBox.Name = "databaseTypeComboBox";
            this.databaseTypeComboBox.Size = new System.Drawing.Size(121, 21);
            this.databaseTypeComboBox.TabIndex = 7;
            // 
            // dataSourceIDLabel
            // 
            dataSourceIDLabel.AutoSize = true;
            dataSourceIDLabel.Location = new System.Drawing.Point(13, 158);
            dataSourceIDLabel.Name = "dataSourceIDLabel";
            dataSourceIDLabel.Size = new System.Drawing.Size(84, 13);
            dataSourceIDLabel.TabIndex = 8;
            dataSourceIDLabel.Text = "Data Source ID:";
            // 
            // dataSourceIDComboBox
            // 
            this.dataSourceIDComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.entitiesBindingSource, "DataSourceID", true));
            this.dataSourceIDComboBox.FormattingEnabled = true;
            this.dataSourceIDComboBox.Location = new System.Drawing.Point(103, 155);
            this.dataSourceIDComboBox.Name = "dataSourceIDComboBox";
            this.dataSourceIDComboBox.Size = new System.Drawing.Size(121, 21);
            this.dataSourceIDComboBox.TabIndex = 9;
            // 
            // editableLabel
            // 
            editableLabel.AutoSize = true;
            editableLabel.Location = new System.Drawing.Point(49, 187);
            editableLabel.Name = "editableLabel";
            editableLabel.Size = new System.Drawing.Size(48, 13);
            editableLabel.TabIndex = 10;
            editableLabel.Text = "Editable:";
            // 
            // editableCheckBox
            // 
            this.editableCheckBox.DataBindings.Add(new System.Windows.Forms.Binding("CheckState", this.entitiesBindingSource, "Editable", true));
            this.editableCheckBox.Location = new System.Drawing.Point(103, 182);
            this.editableCheckBox.Name = "editableCheckBox";
            this.editableCheckBox.Size = new System.Drawing.Size(104, 24);
            this.editableCheckBox.TabIndex = 11;
            this.editableCheckBox.UseVisualStyleBackColor = true;
            // 
            // keyTokenLabel
            // 
            keyTokenLabel.AutoSize = true;
            keyTokenLabel.Location = new System.Drawing.Point(35, 215);
            keyTokenLabel.Name = "keyTokenLabel";
            keyTokenLabel.Size = new System.Drawing.Size(62, 13);
            keyTokenLabel.TabIndex = 12;
            keyTokenLabel.Text = "Key Token:";
            // 
            // keyTokenTextBox
            // 
            this.keyTokenTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.entitiesBindingSource, "KeyToken", true));
            this.keyTokenTextBox.Location = new System.Drawing.Point(103, 212);
            this.keyTokenTextBox.Name = "keyTokenTextBox";
            this.keyTokenTextBox.Size = new System.Drawing.Size(100, 20);
            this.keyTokenTextBox.TabIndex = 13;
            // 
            // parentIdLabel
            // 
            parentIdLabel.AutoSize = true;
            parentIdLabel.Location = new System.Drawing.Point(44, 51);
            parentIdLabel.Name = "parentIdLabel";
            parentIdLabel.Size = new System.Drawing.Size(53, 13);
            parentIdLabel.TabIndex = 14;
            parentIdLabel.Text = "Parent Id:";
            // 
            // parentIdComboBox
            // 
            this.parentIdComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.entitiesBindingSource, "ParentId", true));
            this.parentIdComboBox.FormattingEnabled = true;
            this.parentIdComboBox.Location = new System.Drawing.Point(103, 48);
            this.parentIdComboBox.Name = "parentIdComboBox";
            this.parentIdComboBox.Size = new System.Drawing.Size(121, 21);
            this.parentIdComboBox.TabIndex = 15;
            // 
            // showLabel
            // 
            showLabel.AutoSize = true;
            showLabel.Location = new System.Drawing.Point(60, 243);
            showLabel.Name = "showLabel";
            showLabel.Size = new System.Drawing.Size(37, 13);
            showLabel.TabIndex = 16;
            showLabel.Text = "Show:";
            // 
            // showCheckBox
            // 
            this.showCheckBox.DataBindings.Add(new System.Windows.Forms.Binding("CheckState", this.entitiesBindingSource, "Show", true));
            this.showCheckBox.Location = new System.Drawing.Point(103, 238);
            this.showCheckBox.Name = "showCheckBox";
            this.showCheckBox.Size = new System.Drawing.Size(104, 24);
            this.showCheckBox.TabIndex = 17;
            this.showCheckBox.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(customBuildQueryLabel);
            this.panel1.Controls.Add(this.customBuildQueryTextBox);
            this.panel1.Controls.Add(this.idTextBox);
            this.panel1.Controls.Add(showLabel);
            this.panel1.Controls.Add(idLabel);
            this.panel1.Controls.Add(this.showCheckBox);
            this.panel1.Controls.Add(this.entityNameTextBox);
            this.panel1.Controls.Add(parentIdLabel);
            this.panel1.Controls.Add(entityNameLabel);
            this.panel1.Controls.Add(this.parentIdComboBox);
            this.panel1.Controls.Add(this.categoryComboBox);
            this.panel1.Controls.Add(keyTokenLabel);
            this.panel1.Controls.Add(categoryLabel);
            this.panel1.Controls.Add(this.keyTokenTextBox);
            this.panel1.Controls.Add(this.databaseTypeComboBox);
            this.panel1.Controls.Add(editableLabel);
            this.panel1.Controls.Add(databaseTypeLabel);
            this.panel1.Controls.Add(this.editableCheckBox);
            this.panel1.Controls.Add(this.dataSourceIDComboBox);
            this.panel1.Controls.Add(dataSourceIDLabel);
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(693, 282);
            this.panel1.TabIndex = 18;
            // 
            // customBuildQueryLabel
            // 
            customBuildQueryLabel.AutoSize = true;
            customBuildQueryLabel.Location = new System.Drawing.Point(286, 25);
            customBuildQueryLabel.Name = "customBuildQueryLabel";
            customBuildQueryLabel.Size = new System.Drawing.Size(102, 13);
            customBuildQueryLabel.TabIndex = 18;
            customBuildQueryLabel.Text = "Custom Build Query:";
            // 
            // customBuildQueryTextBox
            // 
            this.customBuildQueryTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.entitiesBindingSource, "CustomBuildQuery", true));
            this.customBuildQueryTextBox.Location = new System.Drawing.Point(394, 22);
            this.customBuildQueryTextBox.Multiline = true;
            this.customBuildQueryTextBox.Name = "customBuildQueryTextBox";
            this.customBuildQueryTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.customBuildQueryTextBox.Size = new System.Drawing.Size(281, 234);
            this.customBuildQueryTextBox.TabIndex = 19;
            // 
            // uc_updateEntity
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel1);
            this.Name = "uc_updateEntity";
            this.Size = new System.Drawing.Size(699, 812);
            ((System.ComponentModel.ISupportInitialize)(this.entitiesBindingSource)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.BindingSource entitiesBindingSource;
        private System.Windows.Forms.TextBox idTextBox;
        private System.Windows.Forms.TextBox entityNameTextBox;
        private System.Windows.Forms.ComboBox categoryComboBox;
        private System.Windows.Forms.ComboBox databaseTypeComboBox;
        private System.Windows.Forms.ComboBox dataSourceIDComboBox;
        private System.Windows.Forms.CheckBox editableCheckBox;
        private System.Windows.Forms.TextBox keyTokenTextBox;
        private System.Windows.Forms.ComboBox parentIdComboBox;
        private System.Windows.Forms.CheckBox showCheckBox;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox customBuildQueryTextBox;
    }
}
