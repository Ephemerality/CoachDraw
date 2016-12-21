using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic;
using System.Text;
using System.Windows.Forms;

namespace CoachDraw
{
    public partial class frmManage : Form
    {
        private BindingList<Tuple<string, string>> categories = new BindingList<Tuple<string, string>>();

        public frmManage()
        {
            InitializeComponent();
            dgvFiles.CellValueChanged += dgvFiles_CellValueChanged;
        }

        private void frmLoadPLY_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.playDir == "")
            {
                if (!Directory.Exists(Environment.CurrentDirectory + @"\PLAYS"))
                    Directory.CreateDirectory(Environment.CurrentDirectory + @"\PLAYS");
                Properties.Settings.Default.playDir = Environment.CurrentDirectory + @"\PLAYS";
            }
            lstCategories.DataSource = categories;
            lstCategories.DisplayMember = "Item1";
            lstCategories.ValueMember = "Item2";
            LoadCategories(Properties.Settings.Default.playDir);
        }

        private void LoadCategories(string dir)
        {
            if (!Directory.Exists(dir)) return;
            categories.Clear();
            lstCategories.SelectedIndexChanged -= lstCategories_SelectedIndexChanged;
            List<string> dirs = new List<string>(Directory.EnumerateDirectories(dir));
            foreach (string category in dirs)
            {
                categories.Add(new Tuple<string, string>(Path.GetFileName(category), category));
            }
            lstCategories.SelectedIndexChanged += lstCategories_SelectedIndexChanged;
            LoadPlays(categories[0].Item2);
        }

        private void LoadPlays(string dir)
        {
            dgvFiles.CellValueChanged -= dgvFiles_CellValueChanged;
            dgvFiles.Rows.Clear();
            List<string> files = new List<string>(Directory.EnumerateFiles(dir));
            foreach (string file in files)
            {
                if (Path.GetExtension(file).ToUpper() != ".PLYX") continue;
                dgvFiles.Rows.Add(Path.GetFileNameWithoutExtension(file), Plays.GetPLYXName(file), file);
            }
            dgvFiles.CellValueChanged += dgvFiles_CellValueChanged;
        }

        private void lstCategories_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadPlays(((Tuple<string, string>)lstCategories.SelectedItem).Item2);
        }

        private string StripInvalid(string input)
        {
            return Path.GetInvalidFileNameChars().Aggregate(input, (current, c) => current.Replace(c, '-'));
        }

        private void btnNewCat_Click(object sender, EventArgs e)
        {
            string result = Interaction.InputBox("Enter a category name.\r\nCannot contain the follow characters:\r\n\" , < > | : * ? \\ /", "New Category");
            result = StripInvalid(result).ToUpper();
            if (result == "") return;
            foreach (Tuple<string, string> r in lstCategories.Items)
            {
                if (r.Item1 == result)
                {
                    MessageBox.Show("Category already exists.", "Invalid Name");
                    return;
                }
                else if (Properties.Settings.Default.playDir.Length + result.Length > 240)
                {
                    MessageBox.Show("Category name is too long.", "Invalid Name");
                    return;
                }
            }
            try
            {
                Directory.CreateDirectory(Properties.Settings.Default.playDir + @"\" + result);
                Tuple<string, string> newCategory = new Tuple<string, string>(result, Properties.Settings.Default.playDir + @"\" + result);
                categories.Add(newCategory);
                lstCategories.SelectedItem = newCategory;
                LoadPlays(newCategory.Item2);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating category:\r\n" + ex.ToString());
            }
        }

        private void btnRenCat_Click(object sender, EventArgs e)
        {
            if (lstCategories.SelectedItem == null) return;
            string result = Interaction.InputBox("Enter a new category name.\r\nCannot contain the follow characters:\r\n\" , < > | : * ? \\ /", "Rename Category", ((Tuple<string, string>)lstCategories.SelectedItem).Item1);
            result = StripInvalid(result).ToUpper();
            if (result == "") return;
            foreach (Tuple<string, string> r in lstCategories.Items)
            {
                if (r.Item1 == result)
                {
                    MessageBox.Show("Category already exists.", "Invalid Name");
                    return;
                }
                else if (Properties.Settings.Default.playDir.Length + result.Length > 240)
                {
                    MessageBox.Show("Category name is too long.", "Invalid Name");
                    return;
                }
            }
            try
            {
                DirectoryInfo di = new DirectoryInfo(((Tuple<string, string>)lstCategories.SelectedItem).Item2);
                di.MoveTo(Path.Combine(di.Parent.FullName, result));
                Tuple<string, string> newCategory = new Tuple<string, string>(di.Name, di.FullName);
                categories.Remove((Tuple<string, string>)lstCategories.SelectedItem);
                categories.Add(newCategory);
                lstCategories.SelectedItem = newCategory;
                LoadPlays(newCategory.Item2);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error renaming category:\r\n" + ex.ToString());
            }
        }

        private void btnDelCat_Click(object sender, EventArgs e)
        {
            if (lstCategories.SelectedItem == null) return;
            Tuple<string, string> current = (Tuple<string, string>)lstCategories.SelectedItem;
            if (Directory.EnumerateFiles(current.Item2).Count() > 0)
            {
                MessageBox.Show("Category is not empty!");
                return;
            }
            try
            {
                Directory.Delete(current.Item2);
                categories.Remove(current);
                LoadPlays(((Tuple<string, string>)lstCategories.SelectedItem).Item2);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting category:\r\n" + ex.ToString());
            }
        }

        private void dgvFiles_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0) // Change filename
            {
                try
                {
                    FileInfo fi = new FileInfo((string)dgvFiles.Rows[e.RowIndex].Cells[2].Value);
                    fi.MoveTo(Path.Combine(fi.DirectoryName, (string)dgvFiles.Rows[e.RowIndex].Cells[0].Value + fi.Extension));
                    dgvFiles.Rows[e.RowIndex].Cells[2].Value = fi.FullName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error renaming play file:\r\n" + ex.ToString());
                    dgvFiles.CellValueChanged -= dgvFiles_CellValueChanged;
                    dgvFiles.Rows[e.RowIndex].Cells[0].Value = Path.GetFileNameWithoutExtension((string)dgvFiles.Rows[e.RowIndex].Cells[2].Value);
                    dgvFiles.CellValueChanged += dgvFiles_CellValueChanged;
                }
            }
            else if (e.ColumnIndex == 1) // Change play name
            {
                try
                {
                    if (!Plays.RenamePLYX((string)dgvFiles.Rows[e.RowIndex].Cells[2].Value, (string)dgvFiles.Rows[e.RowIndex].Cells[1].Value))
                    {
                        dgvFiles.Rows[e.RowIndex].Cells[1].Value = Plays.GetPLYXName((string)dgvFiles.Rows[e.RowIndex].Cells[2].Value);
                        MessageBox.Show("Renaming play failed, play file is not valid.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error renaming play:\r\n" + ex.ToString());
                }
            }
        }

        private void dgvFiles_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (MessageBox.Show(String.Format("Are you sure you want to delete this play? This cannot be undone.\r\nFile: {0}\r\nPlay: {1}", e.Row.Cells[0].Value, e.Row.Cells[1].Value), "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                try
                {
                    File.Delete((string)e.Row.Cells[2].Value);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting play:\r\n" + ex.ToString());
                }
            }
            else
                e.Cancel = true;
        }
    }
}
