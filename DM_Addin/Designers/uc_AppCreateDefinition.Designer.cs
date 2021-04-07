
namespace TheTechIdea.ETL
{
    partial class uc_AppCreateDefinition
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
            System.Windows.Forms.Label appNameLabel;
            System.Windows.Forms.Label createDateLabel;
            System.Windows.Forms.Label ouputFolderLabel;
            System.Windows.Forms.Label verLabel;
            System.Windows.Forms.Label apptypeLabel;
            System.Windows.Forms.Label label1;
            System.Windows.Forms.Label label2;
            System.Windows.Forms.Label label3;
            System.Windows.Forms.Label generatorNameLabel;
            System.Windows.Forms.Label label4;
            this.appNameTextBox = new System.Windows.Forms.TextBox();
            this.appsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.createDateDateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.appVersionsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.ouputFolderTextBox = new System.Windows.Forms.TextBox();
            this.verTextBox = new System.Windows.Forms.TextBox();
            this.Generatebutton = new System.Windows.Forms.Button();
            this.apptypeComboBox = new System.Windows.Forms.ComboBox();
            this.Folderbutton = new System.Windows.Forms.Button();
            this.generatorNameComboBox = new System.Windows.Forms.ComboBox();
            appNameLabel = new System.Windows.Forms.Label();
            createDateLabel = new System.Windows.Forms.Label();
            ouputFolderLabel = new System.Windows.Forms.Label();
            verLabel = new System.Windows.Forms.Label();
            apptypeLabel = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            generatorNameLabel = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.appsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.appVersionsBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // appNameLabel
            // 
            appNameLabel.AutoSize = true;
            appNameLabel.Location = new System.Drawing.Point(40, 66);
            appNameLabel.Name = "appNameLabel";
            appNameLabel.Size = new System.Drawing.Size(60, 13);
            appNameLabel.TabIndex = 1;
            appNameLabel.Text = "App Name:";
            // 
            // createDateLabel
            // 
            createDateLabel.AutoSize = true;
            createDateLabel.Location = new System.Drawing.Point(33, 126);
            createDateLabel.Name = "createDateLabel";
            createDateLabel.Size = new System.Drawing.Size(67, 13);
            createDateLabel.TabIndex = 3;
            createDateLabel.Text = "Create Date:";
            // 
            // ouputFolderLabel
            // 
            ouputFolderLabel.AutoSize = true;
            ouputFolderLabel.Location = new System.Drawing.Point(29, 151);
            ouputFolderLabel.Name = "ouputFolderLabel";
            ouputFolderLabel.Size = new System.Drawing.Size(71, 13);
            ouputFolderLabel.TabIndex = 5;
            ouputFolderLabel.Text = "Ouput Folder:";
            // 
            // verLabel
            // 
            verLabel.AutoSize = true;
            verLabel.Location = new System.Drawing.Point(74, 99);
            verLabel.Name = "verLabel";
            verLabel.Size = new System.Drawing.Size(26, 13);
            verLabel.TabIndex = 7;
            verLabel.Text = "Ver:";
            // 
            // apptypeLabel
            // 
            apptypeLabel.AutoSize = true;
            apptypeLabel.Location = new System.Drawing.Point(66, 204);
            apptypeLabel.Name = "apptypeLabel";
            apptypeLabel.Size = new System.Drawing.Size(34, 13);
            apptypeLabel.TabIndex = 10;
            apptypeLabel.Text = "Type:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(351, 146);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(11, 13);
            label1.TabIndex = 13;
            label1.Text = "*";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(351, 66);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(11, 13);
            label2.TabIndex = 14;
            label2.Text = "*";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new System.Drawing.Font("Impact", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(254)));
            label3.Location = new System.Drawing.Point(111, 28);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(237, 23);
            label3.TabIndex = 19;
            label3.Text = "Create Application Definition";
            // 
            // generatorNameLabel
            // 
            generatorNameLabel.AutoSize = true;
            generatorNameLabel.Location = new System.Drawing.Point(40, 177);
            generatorNameLabel.Name = "generatorNameLabel";
            generatorNameLabel.Size = new System.Drawing.Size(57, 13);
            generatorNameLabel.TabIndex = 19;
            generatorNameLabel.Text = "Generator:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(351, 177);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(11, 13);
            label4.TabIndex = 21;
            label4.Text = "*";
            // 
            // appNameTextBox
            // 
            this.appNameTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.appsBindingSource, "AppName", true));
            this.appNameTextBox.Enabled = false;
            this.appNameTextBox.Location = new System.Drawing.Point(106, 63);
            this.appNameTextBox.Name = "appNameTextBox";
            this.appNameTextBox.Size = new System.Drawing.Size(241, 20);
            this.appNameTextBox.TabIndex = 0;
            // 
            // appsBindingSource
            // 
            this.appsBindingSource.DataSource = typeof(TheTechIdea.DataManagment_Engine.AppBuilder.App);
            // 
            // createDateDateTimePicker
            // 
            this.createDateDateTimePicker.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.appsBindingSource, "CreateDate", true));
            this.createDateDateTimePicker.Enabled = false;
            this.createDateDateTimePicker.Location = new System.Drawing.Point(106, 122);
            this.createDateDateTimePicker.Name = "createDateDateTimePicker";
            this.createDateDateTimePicker.Size = new System.Drawing.Size(241, 20);
            this.createDateDateTimePicker.TabIndex = 2;
            // 
            // appVersionsBindingSource
            // 
            this.appVersionsBindingSource.AllowNew = true;
            this.appVersionsBindingSource.DataMember = "AppVersions";
            this.appVersionsBindingSource.DataSource = this.appsBindingSource;
            // 
            // ouputFolderTextBox
            // 
            this.ouputFolderTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.appVersionsBindingSource, "OuputFolder", true));
            this.ouputFolderTextBox.Location = new System.Drawing.Point(106, 148);
            this.ouputFolderTextBox.Name = "ouputFolderTextBox";
            this.ouputFolderTextBox.Size = new System.Drawing.Size(241, 20);
            this.ouputFolderTextBox.TabIndex = 3;
            // 
            // verTextBox
            // 
            this.verTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.appVersionsBindingSource, "Ver", true));
            this.verTextBox.Enabled = false;
            this.verTextBox.Location = new System.Drawing.Point(106, 96);
            this.verTextBox.Name = "verTextBox";
            this.verTextBox.Size = new System.Drawing.Size(75, 20);
            this.verTextBox.TabIndex = 1;
            // 
            // Generatebutton
            // 
            this.Generatebutton.Location = new System.Drawing.Point(106, 228);
            this.Generatebutton.Name = "Generatebutton";
            this.Generatebutton.Size = new System.Drawing.Size(242, 23);
            this.Generatebutton.TabIndex = 7;
            this.Generatebutton.Text = "Save Definition";
            this.Generatebutton.UseVisualStyleBackColor = true;
            // 
            // apptypeComboBox
            // 
            this.apptypeComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.appVersionsBindingSource, "Apptype", true));
            this.apptypeComboBox.FormattingEnabled = true;
            this.apptypeComboBox.Location = new System.Drawing.Point(106, 201);
            this.apptypeComboBox.Name = "apptypeComboBox";
            this.apptypeComboBox.Size = new System.Drawing.Size(121, 21);
            this.apptypeComboBox.TabIndex = 6;
            // 
            // Folderbutton
            // 
            this.Folderbutton.Location = new System.Drawing.Point(368, 146);
            this.Folderbutton.Name = "Folderbutton";
            this.Folderbutton.Size = new System.Drawing.Size(60, 23);
            this.Folderbutton.TabIndex = 4;
            this.Folderbutton.Text = "Folder";
            this.Folderbutton.UseVisualStyleBackColor = true;
            // 
            // generatorNameComboBox
            // 
            this.generatorNameComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.appVersionsBindingSource, "GeneratorName", true));
            this.generatorNameComboBox.FormattingEnabled = true;
            this.generatorNameComboBox.Location = new System.Drawing.Point(106, 174);
            this.generatorNameComboBox.Name = "generatorNameComboBox";
            this.generatorNameComboBox.Size = new System.Drawing.Size(241, 21);
            this.generatorNameComboBox.TabIndex = 5;
            // 
            // uc_AppCreateDefinition
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(label4);
            this.Controls.Add(generatorNameLabel);
            this.Controls.Add(this.generatorNameComboBox);
            this.Controls.Add(label3);
            this.Controls.Add(label2);
            this.Controls.Add(label1);
            this.Controls.Add(this.Folderbutton);
            this.Controls.Add(apptypeLabel);
            this.Controls.Add(this.apptypeComboBox);
            this.Controls.Add(this.Generatebutton);
            this.Controls.Add(verLabel);
            this.Controls.Add(this.verTextBox);
            this.Controls.Add(ouputFolderLabel);
            this.Controls.Add(this.ouputFolderTextBox);
            this.Controls.Add(createDateLabel);
            this.Controls.Add(this.createDateDateTimePicker);
            this.Controls.Add(appNameLabel);
            this.Controls.Add(this.appNameTextBox);
            this.Name = "uc_AppCreateDefinition";
            this.Size = new System.Drawing.Size(448, 277);
           
            ((System.ComponentModel.ISupportInitialize)(this.appsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.appVersionsBindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox appNameTextBox;
        private System.Windows.Forms.DateTimePicker createDateDateTimePicker;
        private System.Windows.Forms.TextBox ouputFolderTextBox;
        private System.Windows.Forms.TextBox verTextBox;
        private System.Windows.Forms.Button Generatebutton;
        private System.Windows.Forms.ComboBox apptypeComboBox;
        private System.Windows.Forms.Button Folderbutton;
        private System.Windows.Forms.ComboBox generatorNameComboBox;
        public System.Windows.Forms.BindingSource appsBindingSource;
        public System.Windows.Forms.BindingSource appVersionsBindingSource;
    }
}
