
namespace TheTechIdea.ETL
{
    partial class uc_copydatasource
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
            System.Windows.Forms.Label scriptSourceLabel;
            System.Windows.Forms.Label workflowLabel;
            this.dMEEditorBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.idTextBox = new System.Windows.Forms.TextBox();
            this.scriptSourceTextBox = new System.Windows.Forms.TextBox();
            this.workflowTextBox = new System.Windows.Forms.TextBox();
            idLabel = new System.Windows.Forms.Label();
            scriptSourceLabel = new System.Windows.Forms.Label();
            workflowLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dMEEditorBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // dMEEditorBindingSource
            // 
            this.dMEEditorBindingSource.DataSource = typeof(TheTechIdea.DataManagment_Engine.DMEEditor);
            // 
            // idLabel
            // 
            idLabel.AutoSize = true;
            idLabel.Location = new System.Drawing.Point(98, 71);
            idLabel.Name = "idLabel";
            idLabel.Size = new System.Drawing.Size(18, 13);
            idLabel.TabIndex = 1;
            idLabel.Text = "id:";
            // 
            // idTextBox
            // 
            this.idTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.dMEEditorBindingSource, "ETL.script.id", true));
            this.idTextBox.Location = new System.Drawing.Point(176, 68);
            this.idTextBox.Name = "idTextBox";
            this.idTextBox.Size = new System.Drawing.Size(100, 20);
            this.idTextBox.TabIndex = 2;
            // 
            // scriptSourceLabel
            // 
            scriptSourceLabel.AutoSize = true;
            scriptSourceLabel.Location = new System.Drawing.Point(98, 97);
            scriptSourceLabel.Name = "scriptSourceLabel";
            scriptSourceLabel.Size = new System.Drawing.Size(72, 13);
            scriptSourceLabel.TabIndex = 3;
            scriptSourceLabel.Text = "script Source:";
            // 
            // scriptSourceTextBox
            // 
            this.scriptSourceTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.dMEEditorBindingSource, "ETL.script.scriptSource", true));
            this.scriptSourceTextBox.Location = new System.Drawing.Point(176, 94);
            this.scriptSourceTextBox.Name = "scriptSourceTextBox";
            this.scriptSourceTextBox.Size = new System.Drawing.Size(100, 20);
            this.scriptSourceTextBox.TabIndex = 4;
            // 
            // workflowLabel
            // 
            workflowLabel.AutoSize = true;
            workflowLabel.Location = new System.Drawing.Point(98, 123);
            workflowLabel.Name = "workflowLabel";
            workflowLabel.Size = new System.Drawing.Size(52, 13);
            workflowLabel.TabIndex = 5;
            workflowLabel.Text = "workflow:";
            // 
            // workflowTextBox
            // 
            this.workflowTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.dMEEditorBindingSource, "ETL.script.workflow", true));
            this.workflowTextBox.Location = new System.Drawing.Point(176, 120);
            this.workflowTextBox.Name = "workflowTextBox";
            this.workflowTextBox.Size = new System.Drawing.Size(100, 20);
            this.workflowTextBox.TabIndex = 6;
            // 
            // uc_copydatasource
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(idLabel);
            this.Controls.Add(this.idTextBox);
            this.Controls.Add(scriptSourceLabel);
            this.Controls.Add(this.scriptSourceTextBox);
            this.Controls.Add(workflowLabel);
            this.Controls.Add(this.workflowTextBox);
            this.Name = "uc_copydatasource";
            this.Size = new System.Drawing.Size(568, 504);
            ((System.ComponentModel.ISupportInitialize)(this.dMEEditorBindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.BindingSource dMEEditorBindingSource;
        private System.Windows.Forms.TextBox idTextBox;
        private System.Windows.Forms.TextBox scriptSourceTextBox;
        private System.Windows.Forms.TextBox workflowTextBox;
    }
}
