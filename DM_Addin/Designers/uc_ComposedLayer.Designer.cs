
namespace TheTechIdea.ETL
{
    partial class uc_ComposedLayer
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
            System.Windows.Forms.Label layerNameLabel;
            System.Windows.Forms.Label dataViewDataSourceNameLabel;
            System.Windows.Forms.Label dateCreatedLabel;
            System.Windows.Forms.Label dateUpdatedLabel;
            System.Windows.Forms.Label iDLabel;
            System.Windows.Forms.Label localDBDriverLabel;
            System.Windows.Forms.Label localDBDriverVersionLabel;
            System.Windows.Forms.Label label1;
            System.Windows.Forms.Label label2;
            System.Windows.Forms.Label label3;
            this.compositeQueryLayersBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.layerNameTextBox = new System.Windows.Forms.TextBox();
            this.dataViewDataSourceNameComboBox = new System.Windows.Forms.ComboBox();
            this.dateCreatedDateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.dateUpdatedDateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.iDTextBox = new System.Windows.Forms.TextBox();
            this.localDBDriverComboBox = new System.Windows.Forms.ComboBox();
            this.localDBDriverVersionComboBox = new System.Windows.Forms.ComboBox();
            this.Createbutton = new System.Windows.Forms.Button();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.FolderSavelocationlabel = new System.Windows.Forms.Label();
            this.FolderLocationbutton = new System.Windows.Forms.Button();
            this.FileNametextBox = new System.Windows.Forms.TextBox();
            layerNameLabel = new System.Windows.Forms.Label();
            dataViewDataSourceNameLabel = new System.Windows.Forms.Label();
            dateCreatedLabel = new System.Windows.Forms.Label();
            dateUpdatedLabel = new System.Windows.Forms.Label();
            iDLabel = new System.Windows.Forms.Label();
            localDBDriverLabel = new System.Windows.Forms.Label();
            localDBDriverVersionLabel = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.compositeQueryLayersBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // compositeQueryLayersBindingSource
            // 
            this.compositeQueryLayersBindingSource.DataSource = typeof(TheTechIdea.DataManagment_Engine.CompositeLayer.CompositeLayer);
            // 
            // layerNameLabel
            // 
            layerNameLabel.AutoSize = true;
            layerNameLabel.Location = new System.Drawing.Point(128, 89);
            layerNameLabel.Name = "layerNameLabel";
            layerNameLabel.Size = new System.Drawing.Size(67, 13);
            layerNameLabel.TabIndex = 1;
            layerNameLabel.Text = "Layer Name:";
            // 
            // layerNameTextBox
            // 
            this.layerNameTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.compositeQueryLayersBindingSource, "LayerName", true));
            this.layerNameTextBox.Location = new System.Drawing.Point(201, 86);
            this.layerNameTextBox.Name = "layerNameTextBox";
            this.layerNameTextBox.Size = new System.Drawing.Size(309, 20);
            this.layerNameTextBox.TabIndex = 1;
            // 
            // dataViewDataSourceNameLabel
            // 
            dataViewDataSourceNameLabel.AutoSize = true;
            dataViewDataSourceNameLabel.Location = new System.Drawing.Point(42, 115);
            dataViewDataSourceNameLabel.Name = "dataViewDataSourceNameLabel";
            dataViewDataSourceNameLabel.Size = new System.Drawing.Size(153, 13);
            dataViewDataSourceNameLabel.TabIndex = 3;
            dataViewDataSourceNameLabel.Text = "Data View Data Source Name:";
            // 
            // dataViewDataSourceNameComboBox
            // 
            this.dataViewDataSourceNameComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.compositeQueryLayersBindingSource, "DataViewDataSourceName", true));
            this.dataViewDataSourceNameComboBox.Enabled = false;
            this.dataViewDataSourceNameComboBox.FormattingEnabled = true;
            this.dataViewDataSourceNameComboBox.Location = new System.Drawing.Point(201, 112);
            this.dataViewDataSourceNameComboBox.Name = "dataViewDataSourceNameComboBox";
            this.dataViewDataSourceNameComboBox.Size = new System.Drawing.Size(309, 21);
            this.dataViewDataSourceNameComboBox.TabIndex = 2;
            // 
            // dateCreatedLabel
            // 
            dateCreatedLabel.AutoSize = true;
            dateCreatedLabel.Location = new System.Drawing.Point(122, 143);
            dateCreatedLabel.Name = "dateCreatedLabel";
            dateCreatedLabel.Size = new System.Drawing.Size(73, 13);
            dateCreatedLabel.TabIndex = 5;
            dateCreatedLabel.Text = "Date Created:";
            // 
            // dateCreatedDateTimePicker
            // 
            this.dateCreatedDateTimePicker.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.compositeQueryLayersBindingSource, "DateCreated", true));
            this.dateCreatedDateTimePicker.Enabled = false;
            this.dateCreatedDateTimePicker.Location = new System.Drawing.Point(201, 139);
            this.dateCreatedDateTimePicker.Name = "dateCreatedDateTimePicker";
            this.dateCreatedDateTimePicker.Size = new System.Drawing.Size(200, 20);
            this.dateCreatedDateTimePicker.TabIndex = 3;
            // 
            // dateUpdatedLabel
            // 
            dateUpdatedLabel.AutoSize = true;
            dateUpdatedLabel.Location = new System.Drawing.Point(118, 169);
            dateUpdatedLabel.Name = "dateUpdatedLabel";
            dateUpdatedLabel.Size = new System.Drawing.Size(77, 13);
            dateUpdatedLabel.TabIndex = 7;
            dateUpdatedLabel.Text = "Date Updated:";
            // 
            // dateUpdatedDateTimePicker
            // 
            this.dateUpdatedDateTimePicker.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.compositeQueryLayersBindingSource, "DateUpdated", true));
            this.dateUpdatedDateTimePicker.Enabled = false;
            this.dateUpdatedDateTimePicker.Location = new System.Drawing.Point(201, 165);
            this.dateUpdatedDateTimePicker.Name = "dateUpdatedDateTimePicker";
            this.dateUpdatedDateTimePicker.Size = new System.Drawing.Size(200, 20);
            this.dateUpdatedDateTimePicker.TabIndex = 4;
            // 
            // iDLabel
            // 
            iDLabel.AutoSize = true;
            iDLabel.Location = new System.Drawing.Point(174, 63);
            iDLabel.Name = "iDLabel";
            iDLabel.Size = new System.Drawing.Size(21, 13);
            iDLabel.TabIndex = 9;
            iDLabel.Text = "ID:";
            // 
            // iDTextBox
            // 
            this.iDTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.compositeQueryLayersBindingSource, "ID", true));
            this.iDTextBox.Enabled = false;
            this.iDTextBox.Location = new System.Drawing.Point(201, 60);
            this.iDTextBox.Name = "iDTextBox";
            this.iDTextBox.Size = new System.Drawing.Size(309, 20);
            this.iDTextBox.TabIndex = 0;
            // 
            // localDBDriverLabel
            // 
            localDBDriverLabel.AutoSize = true;
            localDBDriverLabel.Location = new System.Drawing.Point(113, 194);
            localDBDriverLabel.Name = "localDBDriverLabel";
            localDBDriverLabel.Size = new System.Drawing.Size(82, 13);
            localDBDriverLabel.TabIndex = 11;
            localDBDriverLabel.Text = "Local DBDriver:";
            // 
            // localDBDriverComboBox
            // 
            this.localDBDriverComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.compositeQueryLayersBindingSource, "LocalDBDriver", true));
            this.localDBDriverComboBox.FormattingEnabled = true;
            this.localDBDriverComboBox.Location = new System.Drawing.Point(201, 191);
            this.localDBDriverComboBox.Name = "localDBDriverComboBox";
            this.localDBDriverComboBox.Size = new System.Drawing.Size(121, 21);
            this.localDBDriverComboBox.TabIndex = 5;
            // 
            // localDBDriverVersionLabel
            // 
            localDBDriverVersionLabel.AutoSize = true;
            localDBDriverVersionLabel.Location = new System.Drawing.Point(75, 221);
            localDBDriverVersionLabel.Name = "localDBDriverVersionLabel";
            localDBDriverVersionLabel.Size = new System.Drawing.Size(120, 13);
            localDBDriverVersionLabel.TabIndex = 13;
            localDBDriverVersionLabel.Text = "Local DBDriver Version:";
            // 
            // localDBDriverVersionComboBox
            // 
            this.localDBDriverVersionComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.compositeQueryLayersBindingSource, "LocalDBDriverVersion", true));
            this.localDBDriverVersionComboBox.FormattingEnabled = true;
            this.localDBDriverVersionComboBox.Location = new System.Drawing.Point(201, 218);
            this.localDBDriverVersionComboBox.Name = "localDBDriverVersionComboBox";
            this.localDBDriverVersionComboBox.Size = new System.Drawing.Size(121, 21);
            this.localDBDriverVersionComboBox.TabIndex = 6;
            // 
            // Createbutton
            // 
            this.Createbutton.Location = new System.Drawing.Point(201, 309);
            this.Createbutton.Name = "Createbutton";
            this.Createbutton.Size = new System.Drawing.Size(121, 39);
            this.Createbutton.TabIndex = 14;
            this.Createbutton.Text = "Create";
            this.Createbutton.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(254)));
            label1.Location = new System.Drawing.Point(197, 12);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(235, 24);
            label1.TabIndex = 15;
            label1.Text = "Create Composed Layer";
            label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // FolderSavelocationlabel
            // 
            this.FolderSavelocationlabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.FolderSavelocationlabel.Location = new System.Drawing.Point(201, 246);
            this.FolderSavelocationlabel.Name = "FolderSavelocationlabel";
            this.FolderSavelocationlabel.Size = new System.Drawing.Size(309, 23);
            this.FolderSavelocationlabel.TabIndex = 16;
            this.FolderSavelocationlabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // FolderLocationbutton
            // 
            this.FolderLocationbutton.Location = new System.Drawing.Point(517, 246);
            this.FolderLocationbutton.Name = "FolderLocationbutton";
            this.FolderLocationbutton.Size = new System.Drawing.Size(75, 23);
            this.FolderLocationbutton.TabIndex = 17;
            this.FolderLocationbutton.Text = "Folder";
            this.FolderLocationbutton.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(87, 251);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(108, 13);
            label2.TabIndex = 18;
            label2.Text = "Save to Local Folder:";
            // 
            // FileNametextBox
            // 
            this.FileNametextBox.Location = new System.Drawing.Point(201, 273);
            this.FileNametextBox.Name = "FileNametextBox";
            this.FileNametextBox.Size = new System.Drawing.Size(309, 20);
            this.FileNametextBox.TabIndex = 19;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(89, 276);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(106, 13);
            label3.TabIndex = 20;
            label3.Text = "Database File Name:";
            // 
            // uc_ComposedLayer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(label3);
            this.Controls.Add(this.FileNametextBox);
            this.Controls.Add(label2);
            this.Controls.Add(this.FolderLocationbutton);
            this.Controls.Add(this.FolderSavelocationlabel);
            this.Controls.Add(label1);
            this.Controls.Add(this.Createbutton);
            this.Controls.Add(localDBDriverVersionLabel);
            this.Controls.Add(this.localDBDriverVersionComboBox);
            this.Controls.Add(localDBDriverLabel);
            this.Controls.Add(this.localDBDriverComboBox);
            this.Controls.Add(iDLabel);
            this.Controls.Add(this.iDTextBox);
            this.Controls.Add(dateUpdatedLabel);
            this.Controls.Add(this.dateUpdatedDateTimePicker);
            this.Controls.Add(dateCreatedLabel);
            this.Controls.Add(this.dateCreatedDateTimePicker);
            this.Controls.Add(dataViewDataSourceNameLabel);
            this.Controls.Add(this.dataViewDataSourceNameComboBox);
            this.Controls.Add(layerNameLabel);
            this.Controls.Add(this.layerNameTextBox);
            this.Name = "uc_ComposedLayer";
            this.Size = new System.Drawing.Size(623, 406);
            ((System.ComponentModel.ISupportInitialize)(this.compositeQueryLayersBindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
      //  private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.BindingSource compositeQueryLayersBindingSource;
        private System.Windows.Forms.TextBox layerNameTextBox;
        private System.Windows.Forms.ComboBox dataViewDataSourceNameComboBox;
        private System.Windows.Forms.DateTimePicker dateCreatedDateTimePicker;
        private System.Windows.Forms.DateTimePicker dateUpdatedDateTimePicker;
        private System.Windows.Forms.TextBox iDTextBox;
        private System.Windows.Forms.ComboBox localDBDriverComboBox;
        private System.Windows.Forms.ComboBox localDBDriverVersionComboBox;
        private System.Windows.Forms.Button Createbutton;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.Label FolderSavelocationlabel;
        private System.Windows.Forms.Button FolderLocationbutton;
        private System.Windows.Forms.TextBox FileNametextBox;
    }
}
