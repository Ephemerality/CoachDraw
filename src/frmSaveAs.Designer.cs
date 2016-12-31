namespace CoachDraw
{
    partial class frmSaveAs
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
            this.btnNewCat = new System.Windows.Forms.Button();
            this.lstCategories = new System.Windows.Forms.ListBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.txtFilename = new System.Windows.Forms.TextBox();
            this.lblFilename = new System.Windows.Forms.Label();
            this.chkGenerate = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtPlayName = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.btnNewCat);
            this.panel1.Controls.Add(this.lstCategories);
            this.panel1.Location = new System.Drawing.Point(4, 4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(140, 174);
            this.panel1.TabIndex = 9;
            // 
            // btnNewCat
            // 
            this.btnNewCat.Location = new System.Drawing.Point(7, 145);
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
            this.lstCategories.Location = new System.Drawing.Point(7, 6);
            this.lstCategories.Name = "lstCategories";
            this.lstCategories.Size = new System.Drawing.Size(125, 134);
            this.lstCategories.Sorted = true;
            this.lstCategories.TabIndex = 7;
            this.lstCategories.SelectedIndexChanged += new System.EventHandler(this.lstCategories_SelectedIndexChanged);
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.txtFilename);
            this.panel2.Controls.Add(this.lblFilename);
            this.panel2.Controls.Add(this.chkGenerate);
            this.panel2.Location = new System.Drawing.Point(150, 46);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(185, 59);
            this.panel2.TabIndex = 10;
            // 
            // txtFilename
            // 
            this.txtFilename.Enabled = false;
            this.txtFilename.Location = new System.Drawing.Point(79, 32);
            this.txtFilename.Name = "txtFilename";
            this.txtFilename.Size = new System.Drawing.Size(100, 20);
            this.txtFilename.TabIndex = 2;
            // 
            // lblFilename
            // 
            this.lblFilename.AutoSize = true;
            this.lblFilename.Enabled = false;
            this.lblFilename.Location = new System.Drawing.Point(3, 35);
            this.lblFilename.Name = "lblFilename";
            this.lblFilename.Size = new System.Drawing.Size(72, 13);
            this.lblFilename.TabIndex = 1;
            this.lblFilename.Text = "Play filename:";
            // 
            // chkGenerate
            // 
            this.chkGenerate.AutoSize = true;
            this.chkGenerate.Checked = true;
            this.chkGenerate.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkGenerate.Location = new System.Drawing.Point(6, 9);
            this.chkGenerate.Name = "chkGenerate";
            this.chkGenerate.Size = new System.Drawing.Size(153, 17);
            this.chkGenerate.TabIndex = 0;
            this.chkGenerate.Text = "Use a generated filename?";
            this.chkGenerate.UseVisualStyleBackColor = true;
            this.chkGenerate.CheckedChanged += new System.EventHandler(this.chkGenerate_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(147, 4);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(92, 13);
            this.label2.TabIndex = 11;
            this.label2.Text = "Descriptive name:";
            // 
            // txtPlayName
            // 
            this.txtPlayName.Location = new System.Drawing.Point(150, 20);
            this.txtPlayName.Name = "txtPlayName";
            this.txtPlayName.Size = new System.Drawing.Size(185, 20);
            this.txtPlayName.TabIndex = 12;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(292, 155);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(43, 23);
            this.btnSave.TabIndex = 13;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // frmSaveAs
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(340, 183);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.txtPlayName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmSaveAs";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Save As";
            this.Load += new System.EventHandler(this.frmSaveAs_Load);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnNewCat;
        private System.Windows.Forms.ListBox lstCategories;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TextBox txtFilename;
        private System.Windows.Forms.Label lblFilename;
        private System.Windows.Forms.CheckBox chkGenerate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtPlayName;
        private System.Windows.Forms.Button btnSave;
    }
}