
namespace TheTechIdea.Configuration
{
    partial class uc_fieldproperty
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
            System.Windows.Forms.Label labelLabel;
            System.Windows.Forms.Label readOnlyLabel;
            System.Windows.Forms.Label fieldTypesLabel;
            System.Windows.Forms.Label checkboxTrueValueLabel;
            System.Windows.Forms.Label checkboxFalseValueLabel;
            System.Windows.Forms.Label lookupEntityLabel;
            System.Windows.Forms.Label lookupDisplayLabel;
            System.Windows.Forms.Label lookupValueLabel;
            this.fieldPropertiesListBox = new System.Windows.Forms.ListBox();
            this.labelTextBox = new System.Windows.Forms.TextBox();
            this.readOnlyCheckBox = new System.Windows.Forms.CheckBox();
            this.displaylabelCheckBox = new System.Windows.Forms.CheckBox();
            this.fieldTypesComboBox = new System.Windows.Forms.ComboBox();
            this.valueRetrievedFromParentCheckBox = new System.Windows.Forms.CheckBox();
            this.checkboxOtherValuesCheckBox = new System.Windows.Forms.CheckBox();
            this.checkboxTrueValueTextBox = new System.Windows.Forms.TextBox();
            this.checkboxFalseValueTextBox = new System.Windows.Forms.TextBox();
            this.enabledCheckBox = new System.Windows.Forms.CheckBox();
            this.autofillCheckBox = new System.Windows.Forms.CheckBox();
            this.lookupEntityTextBox = new System.Windows.Forms.TextBox();
            this.lookupDisplayTextBox = new System.Windows.Forms.TextBox();
            this.lookupValueTextBox = new System.Windows.Forms.TextBox();
            this.Savebutton = new System.Windows.Forms.Button();
            this.appfieldPropertiesBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.enititiesBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.propertiesBindingSource = new System.Windows.Forms.BindingSource(this.components);
            labelLabel = new System.Windows.Forms.Label();
            readOnlyLabel = new System.Windows.Forms.Label();
            fieldTypesLabel = new System.Windows.Forms.Label();
            checkboxTrueValueLabel = new System.Windows.Forms.Label();
            checkboxFalseValueLabel = new System.Windows.Forms.Label();
            lookupEntityLabel = new System.Windows.Forms.Label();
            lookupDisplayLabel = new System.Windows.Forms.Label();
            lookupValueLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.appfieldPropertiesBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.enititiesBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.propertiesBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // labelLabel
            // 
            labelLabel.AutoSize = true;
            labelLabel.Location = new System.Drawing.Point(243, 51);
            labelLabel.Name = "labelLabel";
            labelLabel.Size = new System.Drawing.Size(73, 13);
            labelLabel.TabIndex = 2;
            labelLabel.Text = "Display Label:";
            // 
            // readOnlyLabel
            // 
            readOnlyLabel.AutoSize = true;
            readOnlyLabel.Location = new System.Drawing.Point(316, 76);
            readOnlyLabel.Name = "readOnlyLabel";
            readOnlyLabel.Size = new System.Drawing.Size(0, 13);
            readOnlyLabel.TabIndex = 4;
            // 
            // fieldTypesLabel
            // 
            fieldTypesLabel.AutoSize = true;
            fieldTypesLabel.Location = new System.Drawing.Point(257, 82);
            fieldTypesLabel.Name = "fieldTypesLabel";
            fieldTypesLabel.Size = new System.Drawing.Size(59, 13);
            fieldTypesLabel.TabIndex = 8;
            fieldTypesLabel.Text = "Field Type:";
            // 
            // checkboxTrueValueLabel
            // 
            checkboxTrueValueLabel.AutoSize = true;
            checkboxTrueValueLabel.Location = new System.Drawing.Point(254, 229);
            checkboxTrueValueLabel.Name = "checkboxTrueValueLabel";
            checkboxTrueValueLabel.Size = new System.Drawing.Size(62, 13);
            checkboxTrueValueLabel.TabIndex = 12;
            checkboxTrueValueLabel.Text = "True Value:";
            // 
            // checkboxFalseValueLabel
            // 
            checkboxFalseValueLabel.AutoSize = true;
            checkboxFalseValueLabel.Location = new System.Drawing.Point(374, 229);
            checkboxFalseValueLabel.Name = "checkboxFalseValueLabel";
            checkboxFalseValueLabel.Size = new System.Drawing.Size(65, 13);
            checkboxFalseValueLabel.TabIndex = 13;
            checkboxFalseValueLabel.Text = "False Value:";
            // 
            // lookupEntityLabel
            // 
            lookupEntityLabel.AutoSize = true;
            lookupEntityLabel.Location = new System.Drawing.Point(241, 314);
            lookupEntityLabel.Name = "lookupEntityLabel";
            lookupEntityLabel.Size = new System.Drawing.Size(75, 13);
            lookupEntityLabel.TabIndex = 17;
            lookupEntityLabel.Text = "Lookup Entity:";
            // 
            // lookupDisplayLabel
            // 
            lookupDisplayLabel.AutoSize = true;
            lookupDisplayLabel.Location = new System.Drawing.Point(233, 340);
            lookupDisplayLabel.Name = "lookupDisplayLabel";
            lookupDisplayLabel.Size = new System.Drawing.Size(83, 13);
            lookupDisplayLabel.TabIndex = 19;
            lookupDisplayLabel.Text = "Lookup Display:";
            // 
            // lookupValueLabel
            // 
            lookupValueLabel.AutoSize = true;
            lookupValueLabel.Location = new System.Drawing.Point(240, 366);
            lookupValueLabel.Name = "lookupValueLabel";
            lookupValueLabel.Size = new System.Drawing.Size(76, 13);
            lookupValueLabel.TabIndex = 21;
            lookupValueLabel.Text = "Lookup Value:";
            // 
            // fieldPropertiesListBox
            // 
            this.fieldPropertiesListBox.DataSource = this.propertiesBindingSource;
            this.fieldPropertiesListBox.DisplayMember = "fieldname";
            this.fieldPropertiesListBox.Dock = System.Windows.Forms.DockStyle.Left;
            this.fieldPropertiesListBox.FormattingEnabled = true;
            this.fieldPropertiesListBox.Location = new System.Drawing.Point(0, 0);
            this.fieldPropertiesListBox.Name = "fieldPropertiesListBox";
            this.fieldPropertiesListBox.Size = new System.Drawing.Size(210, 458);
            this.fieldPropertiesListBox.TabIndex = 1;
            this.fieldPropertiesListBox.ValueMember = "fieldname";
            // 
            // labelTextBox
            // 
            this.labelTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.propertiesBindingSource, "label", true));
            this.labelTextBox.Location = new System.Drawing.Point(318, 48);
            this.labelTextBox.Name = "labelTextBox";
            this.labelTextBox.Size = new System.Drawing.Size(205, 20);
            this.labelTextBox.TabIndex = 3;
            // 
            // readOnlyCheckBox
            // 
            this.readOnlyCheckBox.DataBindings.Add(new System.Windows.Forms.Binding("CheckState", this.propertiesBindingSource, "readOnly", true));
            this.readOnlyCheckBox.Location = new System.Drawing.Point(318, 136);
            this.readOnlyCheckBox.Name = "readOnlyCheckBox";
            this.readOnlyCheckBox.Size = new System.Drawing.Size(104, 24);
            this.readOnlyCheckBox.TabIndex = 5;
            this.readOnlyCheckBox.Text = "Read Only";
            this.readOnlyCheckBox.UseVisualStyleBackColor = true;
            // 
            // displaylabelCheckBox
            // 
            this.displaylabelCheckBox.DataBindings.Add(new System.Windows.Forms.Binding("CheckState", this.propertiesBindingSource, "displaylabel", true));
            this.displaylabelCheckBox.Location = new System.Drawing.Point(318, 166);
            this.displaylabelCheckBox.Name = "displaylabelCheckBox";
            this.displaylabelCheckBox.Size = new System.Drawing.Size(104, 24);
            this.displaylabelCheckBox.TabIndex = 7;
            this.displaylabelCheckBox.Text = "Show Label";
            this.displaylabelCheckBox.UseVisualStyleBackColor = true;
            // 
            // fieldTypesComboBox
            // 
            this.fieldTypesComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.propertiesBindingSource, "fieldTypes", true));
            this.fieldTypesComboBox.FormattingEnabled = true;
            this.fieldTypesComboBox.Location = new System.Drawing.Point(318, 79);
            this.fieldTypesComboBox.Name = "fieldTypesComboBox";
            this.fieldTypesComboBox.Size = new System.Drawing.Size(205, 21);
            this.fieldTypesComboBox.TabIndex = 9;
            // 
            // valueRetrievedFromParentCheckBox
            // 
            this.valueRetrievedFromParentCheckBox.DataBindings.Add(new System.Windows.Forms.Binding("CheckState", this.propertiesBindingSource, "ValueRetrievedFromParent", true));
            this.valueRetrievedFromParentCheckBox.Location = new System.Drawing.Point(318, 106);
            this.valueRetrievedFromParentCheckBox.Name = "valueRetrievedFromParentCheckBox";
            this.valueRetrievedFromParentCheckBox.Size = new System.Drawing.Size(205, 24);
            this.valueRetrievedFromParentCheckBox.TabIndex = 11;
            this.valueRetrievedFromParentCheckBox.Text = "Value Retrieved From Parent";
            this.valueRetrievedFromParentCheckBox.UseVisualStyleBackColor = true;
            // 
            // checkboxOtherValuesCheckBox
            // 
            this.checkboxOtherValuesCheckBox.DataBindings.Add(new System.Windows.Forms.Binding("CheckState", this.propertiesBindingSource, "checkboxOtherValues", true));
            this.checkboxOtherValuesCheckBox.Location = new System.Drawing.Point(318, 196);
            this.checkboxOtherValuesCheckBox.Name = "checkboxOtherValuesCheckBox";
            this.checkboxOtherValuesCheckBox.Size = new System.Drawing.Size(205, 24);
            this.checkboxOtherValuesCheckBox.TabIndex = 12;
            this.checkboxOtherValuesCheckBox.Text = "CheckBox Other Values";
            this.checkboxOtherValuesCheckBox.UseVisualStyleBackColor = true;
            // 
            // checkboxTrueValueTextBox
            // 
            this.checkboxTrueValueTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.propertiesBindingSource, "checkboxTrueValue", true));
            this.checkboxTrueValueTextBox.Location = new System.Drawing.Point(318, 225);
            this.checkboxTrueValueTextBox.Name = "checkboxTrueValueTextBox";
            this.checkboxTrueValueTextBox.Size = new System.Drawing.Size(52, 20);
            this.checkboxTrueValueTextBox.TabIndex = 13;
            // 
            // checkboxFalseValueTextBox
            // 
            this.checkboxFalseValueTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.propertiesBindingSource, "checkboxFalseValue", true));
            this.checkboxFalseValueTextBox.Location = new System.Drawing.Point(445, 225);
            this.checkboxFalseValueTextBox.Name = "checkboxFalseValueTextBox";
            this.checkboxFalseValueTextBox.Size = new System.Drawing.Size(57, 20);
            this.checkboxFalseValueTextBox.TabIndex = 14;
            // 
            // enabledCheckBox
            // 
            this.enabledCheckBox.DataBindings.Add(new System.Windows.Forms.Binding("CheckState", this.propertiesBindingSource, "enabled", true));
            this.enabledCheckBox.Location = new System.Drawing.Point(318, 251);
            this.enabledCheckBox.Name = "enabledCheckBox";
            this.enabledCheckBox.Size = new System.Drawing.Size(104, 24);
            this.enabledCheckBox.TabIndex = 16;
            this.enabledCheckBox.Text = "Enabled";
            this.enabledCheckBox.UseVisualStyleBackColor = true;
            // 
            // autofillCheckBox
            // 
            this.autofillCheckBox.DataBindings.Add(new System.Windows.Forms.Binding("CheckState", this.propertiesBindingSource, "autofill", true));
            this.autofillCheckBox.Location = new System.Drawing.Point(318, 281);
            this.autofillCheckBox.Name = "autofillCheckBox";
            this.autofillCheckBox.Size = new System.Drawing.Size(104, 24);
            this.autofillCheckBox.TabIndex = 17;
            this.autofillCheckBox.Text = "AutoFill";
            this.autofillCheckBox.UseVisualStyleBackColor = true;
            // 
            // lookupEntityTextBox
            // 
            this.lookupEntityTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.propertiesBindingSource, "lookupEntity", true));
            this.lookupEntityTextBox.Location = new System.Drawing.Point(318, 311);
            this.lookupEntityTextBox.Name = "lookupEntityTextBox";
            this.lookupEntityTextBox.Size = new System.Drawing.Size(100, 20);
            this.lookupEntityTextBox.TabIndex = 18;
            // 
            // lookupDisplayTextBox
            // 
            this.lookupDisplayTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.propertiesBindingSource, "lookupDisplay", true));
            this.lookupDisplayTextBox.Location = new System.Drawing.Point(318, 337);
            this.lookupDisplayTextBox.Name = "lookupDisplayTextBox";
            this.lookupDisplayTextBox.Size = new System.Drawing.Size(100, 20);
            this.lookupDisplayTextBox.TabIndex = 20;
            // 
            // lookupValueTextBox
            // 
            this.lookupValueTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.propertiesBindingSource, "lookupValue", true));
            this.lookupValueTextBox.Location = new System.Drawing.Point(318, 363);
            this.lookupValueTextBox.Name = "lookupValueTextBox";
            this.lookupValueTextBox.Size = new System.Drawing.Size(100, 20);
            this.lookupValueTextBox.TabIndex = 22;
            // 
            // Savebutton
            // 
            this.Savebutton.Location = new System.Drawing.Point(318, 399);
            this.Savebutton.Name = "Savebutton";
            this.Savebutton.Size = new System.Drawing.Size(205, 23);
            this.Savebutton.TabIndex = 23;
            this.Savebutton.Text = "Save";
            this.Savebutton.UseVisualStyleBackColor = true;
            // 
            // appfieldPropertiesBindingSource
            // 
            this.appfieldPropertiesBindingSource.DataSource = typeof(TheTechIdea.Beep.AppBuilder.DataSourceFieldProperties);
            // 
            // enititiesBindingSource
            // 
            this.enititiesBindingSource.DataMember = "enitities";
            this.enititiesBindingSource.DataSource = this.appfieldPropertiesBindingSource;
            // 
            // propertiesBindingSource
            // 
            this.propertiesBindingSource.DataMember = "properties";
            this.propertiesBindingSource.DataSource = this.enititiesBindingSource;
            // 
            // uc_fieldproperty
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Savebutton);
            this.Controls.Add(lookupValueLabel);
            this.Controls.Add(this.lookupValueTextBox);
            this.Controls.Add(lookupDisplayLabel);
            this.Controls.Add(this.lookupDisplayTextBox);
            this.Controls.Add(lookupEntityLabel);
            this.Controls.Add(this.lookupEntityTextBox);
            this.Controls.Add(this.autofillCheckBox);
            this.Controls.Add(this.enabledCheckBox);
            this.Controls.Add(checkboxFalseValueLabel);
            this.Controls.Add(this.checkboxFalseValueTextBox);
            this.Controls.Add(checkboxTrueValueLabel);
            this.Controls.Add(this.checkboxTrueValueTextBox);
            this.Controls.Add(this.checkboxOtherValuesCheckBox);
            this.Controls.Add(this.valueRetrievedFromParentCheckBox);
            this.Controls.Add(fieldTypesLabel);
            this.Controls.Add(this.fieldTypesComboBox);
            this.Controls.Add(this.displaylabelCheckBox);
            this.Controls.Add(readOnlyLabel);
            this.Controls.Add(this.readOnlyCheckBox);
            this.Controls.Add(labelLabel);
            this.Controls.Add(this.labelTextBox);
            this.Controls.Add(this.fieldPropertiesListBox);
            this.Name = "uc_fieldproperty";
            this.Size = new System.Drawing.Size(578, 458);
            ((System.ComponentModel.ISupportInitialize)(this.appfieldPropertiesBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.enititiesBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.propertiesBindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ListBox fieldPropertiesListBox;
      
        private System.Windows.Forms.TextBox labelTextBox;
        private System.Windows.Forms.CheckBox readOnlyCheckBox;
        private System.Windows.Forms.CheckBox displaylabelCheckBox;
        private System.Windows.Forms.ComboBox fieldTypesComboBox;
        private System.Windows.Forms.CheckBox valueRetrievedFromParentCheckBox;
        private System.Windows.Forms.CheckBox checkboxOtherValuesCheckBox;
        private System.Windows.Forms.TextBox checkboxTrueValueTextBox;
        private System.Windows.Forms.TextBox checkboxFalseValueTextBox;
        private System.Windows.Forms.CheckBox enabledCheckBox;
        private System.Windows.Forms.CheckBox autofillCheckBox;
        private System.Windows.Forms.TextBox lookupEntityTextBox;
        private System.Windows.Forms.TextBox lookupDisplayTextBox;
        private System.Windows.Forms.TextBox lookupValueTextBox;
        private System.Windows.Forms.Button Savebutton;
        private System.Windows.Forms.BindingSource enititiesBindingSource;
        private System.Windows.Forms.BindingSource appfieldPropertiesBindingSource;
        private System.Windows.Forms.BindingSource propertiesBindingSource;
    }
}
