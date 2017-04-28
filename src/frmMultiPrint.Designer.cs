namespace CoachDraw
{
    partial class frmMultiPrint
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.dgvFiles = new System.Windows.Forms.DataGridView();
            this.Filename = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PlayName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FullPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lstCategories = new System.Windows.Forms.ListBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnPrint = new System.Windows.Forms.Button();
            this.txtPlay3 = new System.Windows.Forms.TextBox();
            this.txtPlay2 = new System.Windows.Forms.TextBox();
            this.txtPlay1 = new System.Windows.Forms.TextBox();
            this.btnClear3 = new System.Windows.Forms.Button();
            this.btnClear2 = new System.Windows.Forms.Button();
            this.btnClear1 = new System.Windows.Forms.Button();
            this.btnSet3 = new System.Windows.Forms.Button();
            this.btnSet2 = new System.Windows.Forms.Button();
            this.btnSet1 = new System.Windows.Forms.Button();
            this.txtPlay0 = new System.Windows.Forms.TextBox();
            this.btnClear0 = new System.Windows.Forms.Button();
            this.btnSet0 = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvFiles)).BeginInit();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.dgvFiles);
            this.panel1.Controls.Add(this.lstCategories);
            this.panel1.Location = new System.Drawing.Point(4, 4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(545, 225);
            this.panel1.TabIndex = 10;
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
            this.dgvFiles.Location = new System.Drawing.Point(136, 5);
            this.dgvFiles.MultiSelect = false;
            this.dgvFiles.Name = "dgvFiles";
            this.dgvFiles.RowHeadersVisible = false;
            this.dgvFiles.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvFiles.Size = new System.Drawing.Size(400, 212);
            this.dgvFiles.TabIndex = 10;
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
            // lstCategories
            // 
            this.lstCategories.FormattingEnabled = true;
            this.lstCategories.HorizontalScrollbar = true;
            this.lstCategories.Location = new System.Drawing.Point(5, 5);
            this.lstCategories.Name = "lstCategories";
            this.lstCategories.Size = new System.Drawing.Size(125, 212);
            this.lstCategories.Sorted = true;
            this.lstCategories.TabIndex = 7;
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.btnPrint);
            this.panel2.Controls.Add(this.txtPlay3);
            this.panel2.Controls.Add(this.txtPlay2);
            this.panel2.Controls.Add(this.txtPlay1);
            this.panel2.Controls.Add(this.btnClear3);
            this.panel2.Controls.Add(this.btnClear2);
            this.panel2.Controls.Add(this.btnClear1);
            this.panel2.Controls.Add(this.btnSet3);
            this.panel2.Controls.Add(this.btnSet2);
            this.panel2.Controls.Add(this.btnSet1);
            this.panel2.Controls.Add(this.txtPlay0);
            this.panel2.Controls.Add(this.btnClear0);
            this.panel2.Controls.Add(this.btnSet0);
            this.panel2.Location = new System.Drawing.Point(4, 235);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(545, 105);
            this.panel2.TabIndex = 11;
            // 
            // btnPrint
            // 
            this.btnPrint.Location = new System.Drawing.Point(515, 11);
            this.btnPrint.Name = "btnPrint";
            this.btnPrint.Size = new System.Drawing.Size(22, 79);
            this.btnPrint.TabIndex = 12;
            this.btnPrint.Text = "P\r\nR\r\nI\r\nN\r\nT";
            this.btnPrint.UseVisualStyleBackColor = true;
            this.btnPrint.Click += new System.EventHandler(this.btnPrint_Click);
            // 
            // txtPlay3
            // 
            this.txtPlay3.Location = new System.Drawing.Point(99, 77);
            this.txtPlay3.Name = "txtPlay3";
            this.txtPlay3.Size = new System.Drawing.Size(410, 20);
            this.txtPlay3.TabIndex = 11;
            this.txtPlay3.Tag = "";
            // 
            // txtPlay2
            // 
            this.txtPlay2.Location = new System.Drawing.Point(99, 53);
            this.txtPlay2.Name = "txtPlay2";
            this.txtPlay2.Size = new System.Drawing.Size(410, 20);
            this.txtPlay2.TabIndex = 10;
            this.txtPlay2.Tag = "";
            // 
            // txtPlay1
            // 
            this.txtPlay1.Location = new System.Drawing.Point(99, 29);
            this.txtPlay1.Name = "txtPlay1";
            this.txtPlay1.Size = new System.Drawing.Size(410, 20);
            this.txtPlay1.TabIndex = 9;
            this.txtPlay1.Tag = "";
            // 
            // btnClear3
            // 
            this.btnClear3.Location = new System.Drawing.Point(73, 77);
            this.btnClear3.Name = "btnClear3";
            this.btnClear3.Size = new System.Drawing.Size(20, 20);
            this.btnClear3.TabIndex = 8;
            this.btnClear3.Text = "X";
            this.btnClear3.UseVisualStyleBackColor = true;
            this.btnClear3.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnClear2
            // 
            this.btnClear2.Location = new System.Drawing.Point(73, 53);
            this.btnClear2.Name = "btnClear2";
            this.btnClear2.Size = new System.Drawing.Size(20, 20);
            this.btnClear2.TabIndex = 7;
            this.btnClear2.Text = "X";
            this.btnClear2.UseVisualStyleBackColor = true;
            this.btnClear2.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnClear1
            // 
            this.btnClear1.Location = new System.Drawing.Point(73, 29);
            this.btnClear1.Name = "btnClear1";
            this.btnClear1.Size = new System.Drawing.Size(20, 20);
            this.btnClear1.TabIndex = 6;
            this.btnClear1.Text = "X";
            this.btnClear1.UseVisualStyleBackColor = true;
            this.btnClear1.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnSet3
            // 
            this.btnSet3.Location = new System.Drawing.Point(5, 77);
            this.btnSet3.Name = "btnSet3";
            this.btnSet3.Size = new System.Drawing.Size(65, 20);
            this.btnSet3.TabIndex = 5;
            this.btnSet3.Text = "Set Play 4";
            this.btnSet3.UseVisualStyleBackColor = true;
            this.btnSet3.Click += new System.EventHandler(this.btnSet_Click);
            // 
            // btnSet2
            // 
            this.btnSet2.Location = new System.Drawing.Point(5, 53);
            this.btnSet2.Name = "btnSet2";
            this.btnSet2.Size = new System.Drawing.Size(65, 20);
            this.btnSet2.TabIndex = 4;
            this.btnSet2.Text = "Set Play 3";
            this.btnSet2.UseVisualStyleBackColor = true;
            this.btnSet2.Click += new System.EventHandler(this.btnSet_Click);
            // 
            // btnSet1
            // 
            this.btnSet1.Location = new System.Drawing.Point(5, 29);
            this.btnSet1.Name = "btnSet1";
            this.btnSet1.Size = new System.Drawing.Size(65, 20);
            this.btnSet1.TabIndex = 3;
            this.btnSet1.Text = "Set Play 2";
            this.btnSet1.UseVisualStyleBackColor = true;
            this.btnSet1.Click += new System.EventHandler(this.btnSet_Click);
            // 
            // txtPlay0
            // 
            this.txtPlay0.Location = new System.Drawing.Point(99, 6);
            this.txtPlay0.Name = "txtPlay0";
            this.txtPlay0.Size = new System.Drawing.Size(410, 20);
            this.txtPlay0.TabIndex = 2;
            this.txtPlay0.Tag = "";
            // 
            // btnClear0
            // 
            this.btnClear0.Location = new System.Drawing.Point(73, 5);
            this.btnClear0.Name = "btnClear0";
            this.btnClear0.Size = new System.Drawing.Size(20, 20);
            this.btnClear0.TabIndex = 1;
            this.btnClear0.Text = "X";
            this.btnClear0.UseVisualStyleBackColor = true;
            this.btnClear0.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnSet0
            // 
            this.btnSet0.Location = new System.Drawing.Point(5, 5);
            this.btnSet0.Name = "btnSet0";
            this.btnSet0.Size = new System.Drawing.Size(65, 20);
            this.btnSet0.TabIndex = 0;
            this.btnSet0.Text = "Set Play 1";
            this.btnSet0.UseVisualStyleBackColor = true;
            this.btnSet0.Click += new System.EventHandler(this.btnSet_Click);
            // 
            // frmMultiPrint
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(554, 343);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "frmMultiPrint";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Multiprint";
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvFiles)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ListBox lstCategories;
        private System.Windows.Forms.DataGridView dgvFiles;
        private System.Windows.Forms.DataGridViewTextBoxColumn Filename;
        private System.Windows.Forms.DataGridViewTextBoxColumn PlayName;
        private System.Windows.Forms.DataGridViewTextBoxColumn FullPath;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btnSet3;
        private System.Windows.Forms.Button btnSet2;
        private System.Windows.Forms.Button btnSet1;
        private System.Windows.Forms.TextBox txtPlay0;
        private System.Windows.Forms.Button btnClear0;
        private System.Windows.Forms.Button btnSet0;
        private System.Windows.Forms.Button btnClear3;
        private System.Windows.Forms.Button btnClear2;
        private System.Windows.Forms.Button btnClear1;
        private System.Windows.Forms.TextBox txtPlay3;
        private System.Windows.Forms.TextBox txtPlay2;
        private System.Windows.Forms.TextBox txtPlay1;
        private System.Windows.Forms.Button btnPrint;
    }
}