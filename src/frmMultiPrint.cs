using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CoachDraw
{
    public partial class frmMultiPrint : Form
    {
        private readonly BindingList<Tuple<string, string>> _categories = new BindingList<Tuple<string, string>>();
        public List<string> Plays;
        public string LastSelected = "";
        private readonly Control[] _txtPlay;

        public frmMultiPrint(string currentPlay, string lastPlay, List<string> setPlays)
        {
            InitializeComponent();
            _txtPlay = new Control[] { txtPlay0, txtPlay1, txtPlay2, txtPlay3 };
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
            if (lastPlay == "" && currentPlay != "")
                SelectPlay(currentPlay);
            else
                SelectPlay(lastPlay);
            for (var i = 0; i < setPlays.Count; i++)
            {
                _txtPlay[i].Text = CoachDraw.Plays.GetPLYXName(setPlays[i]);
                _txtPlay[i].Tag = setPlays[0];
            }
        }

        private void SelectPlay(string play)
        {
            if (play == "") return;
            var category = Path.GetDirectoryName(play);
            lstCategories.SelectedItem = _categories.First(r => r.Item2 == category);
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
            dgvFiles.Rows.Clear();
            var files = new List<string>(Directory.EnumerateFiles(dir));
            foreach (var file in files)
            {
                if (Path.GetExtension(file)?.ToUpper() != ".PLYX") continue;
                dgvFiles.Rows.Add(Path.GetFileNameWithoutExtension(file), CoachDraw.Plays.GetPLYXName(file), file);
            }
        }

        private void lstCategories_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadPlays(((Tuple<string, string>)lstCategories.SelectedItem).Item2);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            var index = Convert.ToInt32(((Button)sender).Name.Replace("btnClear", ""));
            _txtPlay[index].Text = "";
            _txtPlay[index].Tag = "";
        }

        private void btnSet_Click(object sender, EventArgs e)
        {
            if (dgvFiles.SelectedCells.Count == 0) return;
            var index = Convert.ToInt32(((Button)sender).Name.Replace("btnSet", ""));
            _txtPlay[index].Text = (string)dgvFiles.SelectedRows[0].Cells[1].Value;
            _txtPlay[index].Tag = (string)dgvFiles.SelectedRows[0].Cells[2].Value;
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            Plays = new List<string> { (string)txtPlay0.Tag, (string)txtPlay1.Tag, (string)txtPlay2.Tag, (string)txtPlay3.Tag };
            if (Plays.All(r => r == null))
            {
                MessageBox.Show("Must have at least 1 play set to print!", "No Plays Selected");
                return;
            }
            LastSelected = dgvFiles.SelectedRows.Count > 0 ? (string)dgvFiles.SelectedRows[0].Cells[2].Value : "";
            DialogResult = DialogResult.Yes;
            Close();
        }
    }
}
