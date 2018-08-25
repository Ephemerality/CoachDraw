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
        private BindingList<Tuple<string, string>> categories = new BindingList<Tuple<string, string>>();
        public string playName = "";
        public string fileName = "";

        public frmSaveAs(string playName)
        {
            InitializeComponent();
            txtPlayName.Text = playName;
        }

        private void btnNewCat_Click(object sender, EventArgs e)
        {
            string result = Interaction.InputBox("Enter a category name.\r\nCannot contain the follow characters:\r\n\" , < > | : * ? \\ /", "New Category");
            result = Utils.StripInvalid(result).ToUpper();
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
                GenerateName(categories[lstCategories.SelectedIndex].Item2);
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
            List<string> files = Directory.EnumerateFiles(categoryPath).Select(r => Path.GetFileNameWithoutExtension(r)).ToList<string>();
            for (uint i = 0; i < UInt32.MaxValue; i++)
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
            categories.Clear();
            lstCategories.SelectedIndexChanged -= lstCategories_SelectedIndexChanged;
            List<string> dirs = new List<string>(Directory.EnumerateDirectories(dir));
            foreach (string category in dirs)
            {
                categories.Add(new Tuple<string, string>(Path.GetFileName(category), category));
            }
            lstCategories.SelectedIndexChanged += lstCategories_SelectedIndexChanged;
            GenerateName(categories[0].Item2);
        }

        private void frmSaveAs_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.playDir == "")
            {
                string playPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CoachDraw");
                if (!Directory.Exists(playPath))
                    Directory.CreateDirectory(playPath);
                Properties.Settings.Default.playDir = playPath;
            }
            lstCategories.DataSource = categories;
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
            GenerateName(categories[lstCategories.SelectedIndex].Item2);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (txtFilename.Text.Trim() == "" || txtPlayName.Text.Trim() == "")
            {
                MessageBox.Show("Play and file names can't be blank!", "Error");
                return;
            }
            playName = txtPlayName.Text.Trim();
            fileName = Path.Combine(categories[lstCategories.SelectedIndex].Item2, txtFilename.Text.Trim().ToUpper());
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
