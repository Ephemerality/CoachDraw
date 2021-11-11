namespace CoachDraw
{
    partial class FrmManage
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmManage));
            this.dgvFiles = new System.Windows.Forms.DataGridView();
            this.Filename = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PlayName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FullPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnDelCat = new System.Windows.Forms.Button();
            this.btnRenCat = new System.Windows.Forms.Button();
            this.btnNewCat = new System.Windows.Forms.Button();
            this.lstCategories = new System.Windows.Forms.ListBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.btnOpen = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvFiles)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvFiles
            // 
            this.dgvFiles.AllowUserToAddRows = false;
            this.dgvFiles.AllowUserToResizeColumns = false;
            this.dgvFiles.AllowUserToResizeRows = false;
            this.dgvFiles.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvFiles.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Filename,
            this.PlayName,
            this.FullPath});
            this.dgvFiles.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dgvFiles.Location = new System.Drawing.Point(150, 4);
            this.dgvFiles.Name = "dgvFiles";
            this.dgvFiles.RowHeadersVisible = false;
            this.dgvFiles.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvFiles.Size = new System.Drawing.Size(400, 207);
            this.dgvFiles.TabIndex = 1;
            this.dgvFiles.CellContentDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvFiles_CellContentDoubleClick);
            this.dgvFiles.UserDeletingRow += new System.Windows.Forms.DataGridViewRowCancelEventHandler(this.dgvFiles_UserDeletingRow);
            this.dgvFiles.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dgvFiles_KeyDown);
            // 
            // Filename
            // 
            this.Filename.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.Filename.HeaderText = "Filename";
            this.Filename.Name = "Filename";
            this.Filename.Width = 74;
            // 
            // PlayName
            // 
            this.PlayName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.PlayName.HeaderText = "Play Name";
            this.PlayName.MinimumWidth = 323;
            this.PlayName.Name = "PlayName";
            this.PlayName.Width = 323;
            // 
            // FullPath
            // 
            this.FullPath.HeaderText = "";
            this.FullPath.Name = "FullPath";
            this.FullPath.Visible = false;
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.btnDelCat);
            this.panel1.Controls.Add(this.btnRenCat);
            this.panel1.Controls.Add(this.btnNewCat);
            this.panel1.Controls.Add(this.lstCategories);
            this.panel1.Location = new System.Drawing.Point(4, 4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(140, 285);
            this.panel1.TabIndex = 8;
            // 
            // btnDelCat
            // 
            this.btnDelCat.Location = new System.Drawing.Point(7, 254);
            this.btnDelCat.Name = "btnDelCat";
            this.btnDelCat.Size = new System.Drawing.Size(125, 22);
            this.btnDelCat.TabIndex = 10;
            this.btnDelCat.Text = "Delete";
            this.btnDelCat.UseVisualStyleBackColor = true;
            this.btnDelCat.Click += new System.EventHandler(this.btnDelCat_Click);
            // 
            // btnRenCat
            // 
            this.btnRenCat.Location = new System.Drawing.Point(7, 233);
            this.btnRenCat.Name = "btnRenCat";
            this.btnRenCat.Size = new System.Drawing.Size(125, 22);
            this.btnRenCat.TabIndex = 9;
            this.btnRenCat.Text = "Rename";
            this.btnRenCat.UseVisualStyleBackColor = true;
            this.btnRenCat.Click += new System.EventHandler(this.btnRenCat_Click);
            // 
            // btnNewCat
            // 
            this.btnNewCat.Location = new System.Drawing.Point(7, 212);
            this.btnNewCat.Name = "btnNewCat";
            this.btnNewCat.Size = new System.Drawing.Size(125, 22);
            this.btnNewCat.TabIndex = 8;
            this.btnNewCat.Text = "New";
            this.btnNewCat.UseVisualStyleBackColor = true;
            this.btnNewCat.Click += new System.EventHandler(this.btnNewCat_Click);
            // 
            // lstCategories
            // 
            this.lstCategories.FormattingEnabled = true;
            this.lstCategories.HorizontalScrollbar = true;
            this.lstCategories.Location = new System.Drawing.Point(7, 5);
            this.lstCategories.Name = "lstCategories";
            this.lstCategories.Size = new System.Drawing.Size(125, 199);
            this.lstCategories.Sorted = true;
            this.lstCategories.TabIndex = 7;
            this.lstCategories.SelectedIndexChanged += new System.EventHandler(this.lstCategories_SelectedIndexChanged);
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.SystemColors.Control;
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox1.Location = new System.Drawing.Point(150, 217);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(400, 72);
            this.textBox1.TabIndex = 11;
            this.textBox1.Text = resources.GetString("textBox1.Text");
            // 
            // btnOpen
            // 
            this.btnOpen.Location = new System.Drawing.Point(469, 258);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(75, 23);
            this.btnOpen.TabIndex = 12;
            this.btnOpen.Text = "Open Play";
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // frmManage
            // 
            this.AcceptButton = this.btnOpen;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(556, 293);
            this.Controls.Add(this.btnOpen);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.dgvFiles);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmManage";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Play Manager";
            this.Load += new System.EventHandler(this.frmManage_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvFiles)).EndInit();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.DataGridView dgvFiles;
        private System.Windows.Forms.DataGridViewTextBoxColumn Filename;
        private System.Windows.Forms.DataGridViewTextBoxColumn PlayName;
        private System.Windows.Forms.DataGridViewTextBoxColumn FullPath;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnDelCat;
        private System.Windows.Forms.Button btnRenCat;
        private System.Windows.Forms.Button btnNewCat;
        private System.Windows.Forms.ListBox lstCategories;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button btnOpen;
    }
}