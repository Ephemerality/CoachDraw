using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic;
using System.Windows.Forms;

namespace CoachDraw
{
    public partial class frmSaveAs : Form
    {
        private readonly BindingList<Tuple<string, string>> _categories = new BindingList<Tuple<string, string>>();
        public string playName = "";
        public string fileName = "";

        public frmSaveAs(string playName)
        {
            InitializeComponent();
            txtPlayName.Text = playName;
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
                Directory.CreateDirectory(Properties.Settings.Default.playDir + @"\" + result);
                var newCategory = new Tuple<string, string>(result, Properties.Settings.Default.playDir + @"\" + result);
                _categories.Add(newCategory);
                lstCategories.SelectedItem = newCategory;
                GenerateName(_categories[lstCategories.SelectedIndex].Item2);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating category:\r\n" + ex);
            }
        }

        private void GenerateName(string categoryPath)
        {
            if (chkGenerate.Checked == false)
            {
                txtFilename.Text = "";
                return;
            }
            var files = Directory.EnumerateFiles(categoryPath).Select(Path.GetFileNameWithoutExtension).ToList();
            for (uint i = 0; i < uint.MaxValue; i++)
            {
                if (!files.Contains(i.ToString("D4")))
                {
                    txtFilename.Text = i.ToString("D4") + ".PLYX";
                    break;
                }
            }
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
            GenerateName(_categories[0].Item2);
        }

        private void frmSaveAs_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.playDir == "")
            {
                var playPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CoachDraw");
                if (!Directory.Exists(playPath))
                    Directory.CreateDirectory(playPath);
                Properties.Settings.Default.playDir = playPath;
            }
            lstCategories.DataSource = _categories;
            lstCategories.DisplayMember = "Item1";
            lstCategories.ValueMember = "Item2";
            LoadCategories(Properties.Settings.Default.playDir);
        }

        private void lstCategories_SelectedIndexChanged(object sender, EventArgs e)
        {
            GenerateName(((Tuple<string, string>)lstCategories.SelectedItem).Item2);
        }

        private void chkGenerate_CheckedChanged(object sender, EventArgs e)
        {
            lblFilename.Enabled = !chkGenerate.Checked;
            txtFilename.Enabled = !chkGenerate.Checked;
            GenerateName(_categories[lstCategories.SelectedIndex].Item2);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (txtFilename.Text.Trim() == "" || txtPlayName.Text.Trim() == "")
            {
                MessageBox.Show("Play and file names can't be blank!", "Error");
                return;
            }
            playName = txtPlayName.Text.Trim();
            fileName = Path.Combine(_categories[lstCategories.SelectedIndex].Item2, txtFilename.Text.Trim().ToUpper());
            if (Path.GetExtension(fileName).ToUpper() != ".PLYX")
                fileName += ".PLYX";
            if (File.Exists(fileName))
            {
                if (DialogResult.No == MessageBox.Show("File already exists. Are you sure you want to overwrite it?\r\nThis cannot be undone.", "File Exists", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2))
                    return;
            }
            DialogResult = DialogResult.Yes;
            Close();
        }
    }
}
