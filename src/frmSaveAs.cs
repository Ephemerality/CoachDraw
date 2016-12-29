using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
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
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating category:\r\n" + ex.ToString());
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
            LoadPlays(categories[0].Item2);
        }

        private void frmSaveAs_Load(object sender, EventArgs e)
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

        private void lstCategories_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadPlays(((Tuple<string, string>)lstCategories.SelectedItem).Item2);
        }
    }
}
