
namespace TheTechIdea.ETL
{
    partial class uc_webapiGetQuery
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
            this.FilterPanel = new System.Windows.Forms.Panel();
            this.gridpanel = new System.Windows.Forms.Panel();
            this.GetDataButton = new System.Windows.Forms.Button();
            this.filtersBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.gridpanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.filtersBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // FilterPanel
            // 
            this.FilterPanel.Location = new System.Drawing.Point(4, 35);
            this.FilterPanel.Name = "FilterPanel";
            this.FilterPanel.Size = new System.Drawing.Size(340, 590);
            this.FilterPanel.TabIndex = 1;
            // 
            // gridpanel
            // 
            this.gridpanel.Controls.Add(this.dataGridView1);
            this.gridpanel.Location = new System.Drawing.Point(351, 35);
            this.gridpanel.Name = "gridpanel";
            this.gridpanel.Size = new System.Drawing.Size(680, 590);
            this.gridpanel.TabIndex = 2;
            // 
            // GetDataButton
            // 
            this.GetDataButton.Location = new System.Drawing.Point(130, 649);
            this.GetDataButton.Name = "GetDataButton";
            this.GetDataButton.Size = new System.Drawing.Size(75, 23);
            this.GetDataButton.TabIndex = 3;
            this.GetDataButton.Text = "button1";
            this.GetDataButton.UseVisualStyleBackColor = true;
            // 
            // filtersBindingSource
            // 
            this.filtersBindingSource.DataSource = typeof(TheTechIdea.Beep.Report.ReportFilter);
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(0, 0);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(680, 590);
            this.dataGridView1.TabIndex = 0;
            // 
            // uc_webapiGetQuery
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.GetDataButton);
            this.Controls.Add(this.gridpanel);
            this.Controls.Add(this.FilterPanel);
            this.Name = "uc_webapiGetQuery";
            this.Size = new System.Drawing.Size(1064, 724);
            this.gridpanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.filtersBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel FilterPanel;
        private System.Windows.Forms.Panel gridpanel;
        private System.Windows.Forms.Button GetDataButton;
        private System.Windows.Forms.BindingSource filtersBindingSource;
        private System.Windows.Forms.DataGridView dataGridView1;
    }
}
