using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoachDraw
{
    public partial class frmMultiPrint : Form
    {
        private BindingList<Tuple<string, string>> categories = new BindingList<Tuple<string, string>>();
        public List<string> plays;
        public string lastSelected = "";
        Control[] txtPlay;

        public frmMultiPrint(string currentPlay, string lastPlay, List<string> setPlays)
        {
            InitializeComponent();
            txtPlay = new Control[] { txtPlay0, txtPlay1, txtPlay2, txtPlay3 };
            if (Properties.Settings.Default.playDir == "")
            {
                string playPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CoachDraw");
                if (!Directory.Exists(playPath))
                    Directory.CreateDirectory(playPath);
                Properties.Settings.Default.playDir = playPath;
                Properties.Settings.Default.Save();
            }
            lstCategories.DataSource = categories;
            lstCategories.DisplayMember = "Item1";
            lstCategories.ValueMember = "Item2";
            LoadCategories(Properties.Settings.Default.playDir);
            if (lastPlay == "" && currentPlay != "")
                SelectPlay(currentPlay);
            else
                SelectPlay(lastPlay);
            for (int i = 0; i < setPlays.Count; i++)
            {
                txtPlay[i].Text = Plays.GetPLYXName(setPlays[i]);
                txtPlay[i].Tag = setPlays[0];
            }
        }
        
        private void SelectPlay(string play)
        {
            if (play == "") return;
            string category = Path.GetDirectoryName(play);
            lstCategories.SelectedItem = categories.First(r => r.Item2 == category);
            dgvFiles.ClearSelection();
            foreach (DataGridViewRow row in dgvFiles.Rows)
            {
                if ((string)row.Cells[2].Value == play)
                {
                    row.Selected = true;
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
            if (categories.Count > 0)
                LoadPlays(categories[0].Item2);
        }

        private void LoadPlays(string dir)
        {
            dgvFiles.Rows.Clear();
            List<string> files = new List<string>(Directory.EnumerateFiles(dir));
            foreach (string file in files)
            {
                if (Path.GetExtension(file).ToUpper() != ".PLYX") continue;
                dgvFiles.Rows.Add(Path.GetFileNameWithoutExtension(file), Plays.GetPLYXName(file), file);
            }
        }

        private void lstCategories_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadPlays(((Tuple<string, string>)lstCategories.SelectedItem).Item2);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            int index = Convert.ToInt32(((Button)sender).Name.Replace("btnClear", ""));
            txtPlay[index].Text = "";
            txtPlay[index].Tag = "";
        }

        private void btnSet_Click(object sender, EventArgs e)
        {
            int index = Convert.ToInt32(((Button)sender).Name.Replace("btnSet", ""));
            txtPlay[index].Text = (string)dgvFiles.SelectedRows[0].Cells[1].Value;
            txtPlay[index].Tag = (string)dgvFiles.SelectedRows[0].Cells[2].Value;
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            plays = new List<string>() { (string)txtPlay0.Tag, (string)txtPlay1.Tag, (string)txtPlay2.Tag, (string)txtPlay3.Tag };
            if (plays.All(r => r == null))
            {
                MessageBox.Show("Must have at least 1 play set to print!", "No Plays Selected");
                return;
            }
            lastSelected = dgvFiles.SelectedRows.Count > 0 ? (string)dgvFiles.SelectedRows[0].Cells[2].Value : "";
            DialogResult = DialogResult.Yes;
            Close();
        }
    }
}
