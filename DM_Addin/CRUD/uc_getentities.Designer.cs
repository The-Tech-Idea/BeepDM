
namespace TheTechIdea.Beep.AppBuilder.UserControls
{
    partial class uc_getentities
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.Filterpanel = new System.Windows.Forms.Panel();
            this.uc_filtercontrol1 = new TheTechIdea.Beep.AppBuilder.UserControls.uc_filtercontrol();
            this.filterTitlepanel = new System.Windows.Forms.Panel();
            this.Filtercaptionlabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.SubmitFilterbutton = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.EntitybindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.gridTitlepanel = new System.Windows.Forms.Panel();
            this.Printbutton = new System.Windows.Forms.Button();
            this.expandbutton = new System.Windows.Forms.Button();
            this.subtitlelabel = new System.Windows.Forms.Label();
            this.EntityNamelabel = new System.Windows.Forms.Label();
            this.EditSelectedbutton = new System.Windows.Forms.Button();
            this.DeleteSelectedbutton = new System.Windows.Forms.Button();
            this.InsertNewEntitybutton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.Filterpanel.SuspendLayout();
            this.filterTitlepanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.EntitybindingSource)).BeginInit();
            this.gridTitlepanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.AutoScroll = true;
            this.splitContainer1.Panel1.Controls.Add(this.Filterpanel);
            this.splitContainer1.Panel1.Controls.Add(this.filterTitlepanel);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.AutoScroll = true;
            this.splitContainer1.Panel2.Controls.Add(this.dataGridView1);
            this.splitContainer1.Panel2.Controls.Add(this.gridTitlepanel);
            this.splitContainer1.Size = new System.Drawing.Size(1800, 1231);
            this.splitContainer1.SplitterDistance = 559;
            this.splitContainer1.SplitterWidth = 6;
            this.splitContainer1.TabIndex = 0;
            // 
            // Filterpanel
            // 
            this.Filterpanel.AutoScroll = true;
            this.Filterpanel.Controls.Add(this.uc_filtercontrol1);
            this.Filterpanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Filterpanel.Location = new System.Drawing.Point(0, 140);
            this.Filterpanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Filterpanel.Name = "Filterpanel";
            this.Filterpanel.Size = new System.Drawing.Size(559, 1091);
            this.Filterpanel.TabIndex = 1;
            // 
            // uc_filtercontrol1
            // 
            this.uc_filtercontrol1.AddinName = null;
            this.uc_filtercontrol1.AutoScroll = true;
            this.uc_filtercontrol1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.uc_filtercontrol1.DefaultCreate = false;
            this.uc_filtercontrol1.Description = null;
            this.uc_filtercontrol1.DllName = null;
            this.uc_filtercontrol1.DllPath = null;
            this.uc_filtercontrol1.DMEEditor = null;
            this.uc_filtercontrol1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uc_filtercontrol1.EntityName = null;
            this.uc_filtercontrol1.EntityStructure = null;
            this.uc_filtercontrol1.ErrorObject = null;
            this.uc_filtercontrol1.Location = new System.Drawing.Point(0, 0);
            this.uc_filtercontrol1.Logger = null;
            this.uc_filtercontrol1.Margin = new System.Windows.Forms.Padding(6, 8, 6, 8);
            this.uc_filtercontrol1.Name = "uc_filtercontrol1";
            this.uc_filtercontrol1.NameSpace = null;
            this.uc_filtercontrol1.ObjectName = null;
            this.uc_filtercontrol1.ObjectType = null;
            this.uc_filtercontrol1.ParentName = null;
            this.uc_filtercontrol1.Passedarg = null;
            this.uc_filtercontrol1.Size = new System.Drawing.Size(559, 1091);
            this.uc_filtercontrol1.TabIndex = 0;
            // 
            // filterTitlepanel
            // 
            this.filterTitlepanel.BackColor = System.Drawing.Color.Gainsboro;
            this.filterTitlepanel.Controls.Add(this.Filtercaptionlabel);
            this.filterTitlepanel.Controls.Add(this.label1);
            this.filterTitlepanel.Controls.Add(this.SubmitFilterbutton);
            this.filterTitlepanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.filterTitlepanel.Location = new System.Drawing.Point(0, 0);
            this.filterTitlepanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.filterTitlepanel.Name = "filterTitlepanel";
            this.filterTitlepanel.Size = new System.Drawing.Size(559, 140);
            this.filterTitlepanel.TabIndex = 0;
            // 
            // Filtercaptionlabel
            // 
            this.Filtercaptionlabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Filtercaptionlabel.Location = new System.Drawing.Point(8, 47);
            this.Filtercaptionlabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Filtercaptionlabel.Name = "Filtercaptionlabel";
            this.Filtercaptionlabel.Size = new System.Drawing.Size(549, 49);
            this.Filtercaptionlabel.TabIndex = 0;
            this.Filtercaptionlabel.Text = "Where id=?";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(4, 4);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(74, 29);
            this.label1.TabIndex = 1;
            this.label1.Text = "Filter";
            // 
            // SubmitFilterbutton
            // 
            this.SubmitFilterbutton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SubmitFilterbutton.Location = new System.Drawing.Point(441, 100);
            this.SubmitFilterbutton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.SubmitFilterbutton.Name = "SubmitFilterbutton";
            this.SubmitFilterbutton.Size = new System.Drawing.Size(112, 35);
            this.SubmitFilterbutton.TabIndex = 0;
            this.SubmitFilterbutton.Text = "Submit";
            this.SubmitFilterbutton.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AutoGenerateColumns = false;
            this.dataGridView1.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.Raised;
            this.dataGridView1.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Sunken;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.DataSource = this.EntitybindingSource;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(0, 140);
            this.dataGridView1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Sunken;
            this.dataGridView1.RowHeadersWidth = 62;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new System.Drawing.Size(1235, 1091);
            this.dataGridView1.TabIndex = 1;
            // 
            // gridTitlepanel
            // 
            this.gridTitlepanel.BackColor = System.Drawing.Color.Gainsboro;
            this.gridTitlepanel.Controls.Add(this.Printbutton);
            this.gridTitlepanel.Controls.Add(this.expandbutton);
            this.gridTitlepanel.Controls.Add(this.subtitlelabel);
            this.gridTitlepanel.Controls.Add(this.EntityNamelabel);
            this.gridTitlepanel.Controls.Add(this.EditSelectedbutton);
            this.gridTitlepanel.Controls.Add(this.DeleteSelectedbutton);
            this.gridTitlepanel.Controls.Add(this.InsertNewEntitybutton);
            this.gridTitlepanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.gridTitlepanel.Location = new System.Drawing.Point(0, 0);
            this.gridTitlepanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.gridTitlepanel.Name = "gridTitlepanel";
            this.gridTitlepanel.Size = new System.Drawing.Size(1235, 140);
            this.gridTitlepanel.TabIndex = 0;
            // 
            // Printbutton
            // 
            this.Printbutton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Printbutton.Location = new System.Drawing.Point(736, 100);
            this.Printbutton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Printbutton.Name = "Printbutton";
            this.Printbutton.Size = new System.Drawing.Size(112, 35);
            this.Printbutton.TabIndex = 7;
            this.Printbutton.Text = "Print";
            this.Printbutton.UseVisualStyleBackColor = true;
            // 
            // expandbutton
            // 
            this.expandbutton.FlatAppearance.BorderSize = 0;
            this.expandbutton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.expandbutton.Location = new System.Drawing.Point(4, 100);
            this.expandbutton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.expandbutton.Name = "expandbutton";
            this.expandbutton.Size = new System.Drawing.Size(38, 35);
            this.expandbutton.TabIndex = 6;
            this.expandbutton.UseVisualStyleBackColor = true;
            // 
            // subtitlelabel
            // 
            this.subtitlelabel.AutoSize = true;
            this.subtitlelabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.subtitlelabel.ForeColor = System.Drawing.Color.SteelBlue;
            this.subtitlelabel.Location = new System.Drawing.Point(21, 59);
            this.subtitlelabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.subtitlelabel.Name = "subtitlelabel";
            this.subtitlelabel.Size = new System.Drawing.Size(110, 20);
            this.subtitlelabel.TabIndex = 5;
            this.subtitlelabel.Text = "Data Source";
            // 
            // EntityNamelabel
            // 
            this.EntityNamelabel.AutoSize = true;
            this.EntityNamelabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.EntityNamelabel.ForeColor = System.Drawing.Color.SteelBlue;
            this.EntityNamelabel.Location = new System.Drawing.Point(21, 11);
            this.EntityNamelabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.EntityNamelabel.Name = "EntityNamelabel";
            this.EntityNamelabel.Size = new System.Drawing.Size(153, 29);
            this.EntityNamelabel.TabIndex = 2;
            this.EntityNamelabel.Text = "Entity Name";
            // 
            // EditSelectedbutton
            // 
            this.EditSelectedbutton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.EditSelectedbutton.Location = new System.Drawing.Point(1101, 100);
            this.EditSelectedbutton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.EditSelectedbutton.Name = "EditSelectedbutton";
            this.EditSelectedbutton.Size = new System.Drawing.Size(112, 35);
            this.EditSelectedbutton.TabIndex = 4;
            this.EditSelectedbutton.Text = "Edit Selected";
            this.EditSelectedbutton.UseVisualStyleBackColor = true;
            // 
            // DeleteSelectedbutton
            // 
            this.DeleteSelectedbutton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.DeleteSelectedbutton.Location = new System.Drawing.Point(979, 100);
            this.DeleteSelectedbutton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.DeleteSelectedbutton.Name = "DeleteSelectedbutton";
            this.DeleteSelectedbutton.Size = new System.Drawing.Size(112, 35);
            this.DeleteSelectedbutton.TabIndex = 3;
            this.DeleteSelectedbutton.Text = "Delete Selected";
            this.DeleteSelectedbutton.UseVisualStyleBackColor = true;
            // 
            // InsertNewEntitybutton
            // 
            this.InsertNewEntitybutton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.InsertNewEntitybutton.Location = new System.Drawing.Point(857, 100);
            this.InsertNewEntitybutton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.InsertNewEntitybutton.Name = "InsertNewEntitybutton";
            this.InsertNewEntitybutton.Size = new System.Drawing.Size(112, 35);
            this.InsertNewEntitybutton.TabIndex = 2;
            this.InsertNewEntitybutton.Text = "New";
            this.InsertNewEntitybutton.UseVisualStyleBackColor = true;
            // 
            // uc_getentities
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "uc_getentities";
            this.Size = new System.Drawing.Size(1800, 1231);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.Filterpanel.ResumeLayout(false);
            this.filterTitlepanel.ResumeLayout(false);
            this.filterTitlepanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.EntitybindingSource)).EndInit();
            this.gridTitlepanel.ResumeLayout(false);
            this.gridTitlepanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Panel filterTitlepanel;
        private System.Windows.Forms.Panel gridTitlepanel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button SubmitFilterbutton;
        private System.Windows.Forms.Label Filtercaptionlabel;
        private System.Windows.Forms.Button EditSelectedbutton;
        private System.Windows.Forms.Button DeleteSelectedbutton;
        private System.Windows.Forms.Button InsertNewEntitybutton;
        private System.Windows.Forms.Label subtitlelabel;
        private System.Windows.Forms.Label EntityNamelabel;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.BindingSource EntitybindingSource;
        private System.Windows.Forms.Button expandbutton;
        private System.Windows.Forms.Panel Filterpanel;
        private uc_filtercontrol uc_filtercontrol1;
        private System.Windows.Forms.Button Printbutton;
    }
}
