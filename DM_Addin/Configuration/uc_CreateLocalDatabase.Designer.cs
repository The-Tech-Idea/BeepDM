
namespace TheTechIdea.Configuration
{
    partial class uc_CreateLocalDatabase
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
            System.Windows.Forms.Label passwordLabel;
            this.InstallFoldercomboBox = new System.Windows.Forms.ComboBox();
            this.EmbeddedDatabaseTypecomboBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.CreateDBbutton = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.dataConnectionsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.passwordTextBox = new System.Windows.Forms.TextBox();
            this.databaseTextBox = new System.Windows.Forms.TextBox();
            passwordLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataConnectionsBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // passwordLabel
            // 
            passwordLabel.AutoSize = true;
            passwordLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            passwordLabel.Location = new System.Drawing.Point(208, 123);
            passwordLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            passwordLabel.Name = "passwordLabel";
            passwordLabel.Size = new System.Drawing.Size(91, 20);
            passwordLabel.TabIndex = 32;
            passwordLabel.Text = "Password:";
            // 
            // InstallFoldercomboBox
            // 
            this.InstallFoldercomboBox.FormattingEnabled = true;
            this.InstallFoldercomboBox.Location = new System.Drawing.Point(309, 77);
            this.InstallFoldercomboBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.InstallFoldercomboBox.Name = "InstallFoldercomboBox";
            this.InstallFoldercomboBox.Size = new System.Drawing.Size(439, 28);
            this.InstallFoldercomboBox.TabIndex = 0;
            // 
            // EmbeddedDatabaseTypecomboBox
            // 
            this.EmbeddedDatabaseTypecomboBox.FormattingEnabled = true;
            this.EmbeddedDatabaseTypecomboBox.Location = new System.Drawing.Point(309, 198);
            this.EmbeddedDatabaseTypecomboBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.EmbeddedDatabaseTypecomboBox.Name = "EmbeddedDatabaseTypecomboBox";
            this.EmbeddedDatabaseTypecomboBox.Size = new System.Drawing.Size(180, 28);
            this.EmbeddedDatabaseTypecomboBox.TabIndex = 6;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(145, 82);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(154, 20);
            this.label1.TabIndex = 2;
            this.label1.Text = "Installation Folder";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(115, 203);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(184, 20);
            this.label2.TabIndex = 3;
            this.label2.Text = "Class  Database Type";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(161, 245);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(138, 20);
            this.label3.TabIndex = 5;
            this.label3.Text = "Database Name";
            // 
            // CreateDBbutton
            // 
            this.CreateDBbutton.Location = new System.Drawing.Point(309, 309);
            this.CreateDBbutton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.CreateDBbutton.Name = "CreateDBbutton";
            this.CreateDBbutton.Size = new System.Drawing.Size(182, 35);
            this.CreateDBbutton.TabIndex = 8;
            this.CreateDBbutton.Text = "Create";
            this.CreateDBbutton.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(304, 16);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(257, 29);
            this.label4.TabIndex = 7;
            this.label4.Text = "Create Local Database";
            // 
            // dataConnectionsBindingSource
            // 
            this.dataConnectionsBindingSource.DataSource = typeof(TheTechIdea.Util.ConnectionProperties);
            // 
            // passwordTextBox
            // 
            this.passwordTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.dataConnectionsBindingSource, "Password", true));
            this.passwordTextBox.Location = new System.Drawing.Point(309, 118);
            this.passwordTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.passwordTextBox.Name = "passwordTextBox";
            this.passwordTextBox.Size = new System.Drawing.Size(180, 26);
            this.passwordTextBox.TabIndex = 28;
            // 
            // databaseTextBox
            // 
            this.databaseTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.dataConnectionsBindingSource, "Database", true));
            this.databaseTextBox.Location = new System.Drawing.Point(309, 240);
            this.databaseTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.databaseTextBox.Name = "databaseTextBox";
            this.databaseTextBox.Size = new System.Drawing.Size(180, 26);
            this.databaseTextBox.TabIndex = 26;
            // 
            // uc_CreateLocalDatabase
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(passwordLabel);
            this.Controls.Add(this.passwordTextBox);
            this.Controls.Add(this.databaseTextBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.CreateDBbutton);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.EmbeddedDatabaseTypecomboBox);
            this.Controls.Add(this.InstallFoldercomboBox);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "uc_CreateLocalDatabase";
            this.Size = new System.Drawing.Size(848, 397);
            ((System.ComponentModel.ISupportInitialize)(this.dataConnectionsBindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox InstallFoldercomboBox;
        private System.Windows.Forms.ComboBox EmbeddedDatabaseTypecomboBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button CreateDBbutton;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.BindingSource dataConnectionsBindingSource;
        private System.Windows.Forms.TextBox passwordTextBox;
        private System.Windows.Forms.TextBox databaseTextBox;
    }
}
