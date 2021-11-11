using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CoachDraw
{
    public partial class FrmManage : Form
    {
        private readonly BindingList<Tuple<string, string>> _categories = new();
        public string OpenPlay = "";

        public FrmManage()
        {
            InitializeComponent();
        }

        private void frmManage_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.playDir == "")
            {
                var playPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CoachDraw");
                if (!Directory.Exists(playPath))
                    Directory.CreateDirectory(playPath);
                Properties.Settings.Default.playDir = playPath;
                Properties.Settings.Default.Save();
            }
            lstCategories.DataSource = _categories;
            lstCategories.DisplayMember = "Item1";
            lstCategories.ValueMember = "Item2";
            LoadCategories(Properties.Settings.Default.playDir);
        }

        private void LoadCategories(string dir)
        {
            if (!Directory.Exists(dir)) return;
            _categories.Clear();
            lstCategories.SelectedIndexChanged -= lstCategories_SelectedIndexChanged;
            var dirs = new List<string>(Directory.EnumerateDirectories(dir));
            foreach (var category in dirs)
            {
                _categories.Add(new Tuple<string, string>(Path.GetFileName(category), category));
            }
            lstCategories.SelectedIndexChanged += lstCategories_SelectedIndexChanged;
            if (_categories.Count > 0)
                LoadPlays(_categories[0].Item2);
        }

        private void LoadPlays(string dir)
        {
            dgvFiles.CellValueChanged -= dgvFiles_CellValueChanged;
            dgvFiles.Rows.Clear();
            var files = new List<string>(Directory.EnumerateFiles(dir));
            foreach (var file in files.Where(file => Path.GetExtension(file)?.ToUpper() == ".PLYX"))
            {
                dgvFiles.Rows.Add(Path.GetFileNameWithoutExtension(file), Plays.GetPlyxName(file), file);
            }
            dgvFiles.CellValueChanged += dgvFiles_CellValueChanged;
        }

        private void lstCategories_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadPlays(((Tuple<string, string>)lstCategories.SelectedItem).Item2);
        }

        private void btnNewCat_Click(object sender, EventArgs e)
        {
            if (DialogResult.Cancel == this.InputBox("Enter a category name.\r\nCannot contain the follow characters:\r\n\" , < > | : * ? \\ /", "New Category", out var result))
                return;

            result = result.StripInvalid().ToUpper();
            if (string.IsNullOrEmpty(result))
                return;
            foreach (Tuple<string, string> r in lstCategories.Items)
            {
                if (r.Item1 == result)
                {
                    MessageBox.Show("Category already exists.", "Invalid Name");
                    return;
                }

                if (Properties.Settings.Default.playDir.Length + result.Length > 240)
                {
                    MessageBox.Show("Category name is too long.", "Invalid Name");
                    return;
                }
            }
            try
            {
                var dir = $@"{Properties.Settings.Default.playDir}\{result}";
                Directory.CreateDirectory(dir);
                var newCategory = new Tuple<string, string>(result, dir);
                _categories.Add(newCategory);
                lstCategories.SelectedItem = newCategory;
                LoadPlays(newCategory.Item2);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating category:\r\n{ex}");
            }
        }

        private void btnRenCat_Click(object sender, EventArgs e)
        {
            if (lstCategories.SelectedItem == null) return;
            if (DialogResult.Cancel != this.InputBox("Enter a new category name.\r\nCannot contain the follow characters:\r\n\" , < > | : * ? \\ /",
                    "Rename Category", out var result, ((Tuple<string, string>)lstCategories.SelectedItem).Item1))
                return;
            result = result.StripInvalid().ToUpper();
            if (string.IsNullOrEmpty(result))
                return;
            foreach (Tuple<string, string> r in lstCategories.Items)
            {
                if (r.Item1 == result)
                {
                    MessageBox.Show("Category already exists.", "Invalid Name");
                    return;
                }

                if (Properties.Settings.Default.playDir.Length + result.Length > 240)
                {
                    MessageBox.Show("Category name is too long.", "Invalid Name");
                    return;
                }
            }
            try
            {
                var di = new DirectoryInfo(((Tuple<string, string>)lstCategories.SelectedItem).Item2);
                di.MoveTo(Path.Combine(di.Parent.FullName, result));
                var newCategory = new Tuple<string, string>(di.Name, di.FullName);
                _categories.Remove((Tuple<string, string>)lstCategories.SelectedItem);
                _categories.Add(newCategory);
                lstCategories.SelectedItem = newCategory;
                LoadPlays(newCategory.Item2);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error renaming category:\r\n{ex}");
            }
        }

        private void btnDelCat_Click(object sender, EventArgs e)
        {
            if (lstCategories.SelectedItem == null) return;
            var current = (Tuple<string, string>)lstCategories.SelectedItem;
            if (Directory.EnumerateFiles(current.Item2).Any())
            {
                MessageBox.Show("Category is not empty!");
                return;
            }
            try
            {
                Directory.Delete(current.Item2);
                _categories.Remove(current);
                LoadPlays(((Tuple<string, string>)lstCategories.SelectedItem).Item2);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting category:\r\n{ex}");
            }
        }

        private void dgvFiles_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0) // Change filename
            {
                try
                {
                    var fi = new FileInfo((string)dgvFiles.Rows[e.RowIndex].Cells[2].Value);
                    fi.MoveTo(Path.Combine(fi.DirectoryName, (string)dgvFiles.Rows[e.RowIndex].Cells[0].Value + fi.Extension));
                    dgvFiles.Rows[e.RowIndex].Cells[2].Value = fi.FullName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error renaming play file:\r\n{ex}");
                    dgvFiles.CellValueChanged -= dgvFiles_CellValueChanged;
                    dgvFiles.Rows[e.RowIndex].Cells[0].Value = Path.GetFileNameWithoutExtension((string)dgvFiles.Rows[e.RowIndex].Cells[2].Value);
                    dgvFiles.CellValueChanged += dgvFiles_CellValueChanged;
                }
            }
            else if (e.ColumnIndex == 1) // Change play name
            {
                try
                {
                    if (!Plays.RenamePlyx((string)dgvFiles.Rows[e.RowIndex].Cells[2].Value, (string)dgvFiles.Rows[e.RowIndex].Cells[1].Value))
                    {
                        dgvFiles.Rows[e.RowIndex].Cells[1].Value = Plays.GetPlyxName((string)dgvFiles.Rows[e.RowIndex].Cells[2].Value);
                        MessageBox.Show("Renaming play failed, play file is not valid.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error renaming play:\r\n{ex}");
                }
            }
        }

        private void dgvFiles_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (MessageBox.Show($"Are you sure you want to delete this play? This cannot be undone.\r\nFile: {e.Row.Cells[0].Value}\r\nPlay: {e.Row.Cells[1].Value}",
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                try
                {
                    File.Delete((string)e.Row.Cells[2].Value);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting play:\r\n{ex}");
                }
            }
            else
                e.Cancel = true;
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenSelected();
        }

        private void dgvFiles_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            OpenSelected();
        }

        private void OpenSelected()
        {
            if (dgvFiles.SelectedRows.Count <= 0) return;
            OpenPlay = (string)dgvFiles.SelectedRows[0].Cells[2].Value;
            DialogResult = DialogResult.Yes;
            Close();
        }

        private void dgvFiles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.F2)
                dgvFiles.BeginEdit(true);
            else if (e.KeyData == Keys.Enter)
                OpenSelected();
        }
    }
}
