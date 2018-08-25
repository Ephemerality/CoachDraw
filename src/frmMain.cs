using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace CoachDraw
{
    public partial class frmMain : Form
    {
        /** Path compacting from snippet on MSDN **/
        [DllImport("shlwapi.dll")]
        static extern bool PathCompactPathEx([Out] StringBuilder pszOut, string szPath, int cchMax, int dwFlags);

        static string compactString(string path, int length)
        {
            StringBuilder sb = new StringBuilder(length + 1);
            PathCompactPathEx(sb, path, length + 1, 0);
            return sb.ToString();
        }
        /******************************************/

        private const int RinkWidth = 1000;
        private const int RinkHeight = 500;
        private Point _startPoint;
        private Point _endPoint;
        private bool _mouseDown;
        private Bitmap _snapshot;
        private Bitmap _tempDraw;
        private readonly List<Point> _tempcoords;
        private List<DrawObj> _objs;
        readonly RinkSpecs curSpecs = new RinkSpecs(5); //Scale
        private string _selectedTool = "Line";
        private string _currentFile = "";
        private bool _saved = true;
        private readonly uint plyxVersion = 1;
        private DrawObj _selected;
        private string _lastSelectedMultiPrint = "";
        private List<string> _lastSetMultiPrint = new List<string> { "", "", "", "" };

        public frmMain()
        {
            InitializeComponent();
            _snapshot = new Bitmap(panel1.ClientRectangle.Width, ClientRectangle.Height);
            _tempDraw = (Bitmap)_snapshot.Clone();
            _tempcoords = new List<Point>();
            _objs = new List<DrawObj>();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            updateRecentFiles();
            ItemTypeBox.SelectedIndex = 0;
            LineTypeBox.SelectedIndex = 0;
            EndTypeBox.SelectedIndex = 0;
            PlayerNumBox.Items.Add("None");
            for (int i = 0; i < 100; i++) PlayerNumBox.Items.Add(i.ToString());
            PlayerNumBox.SelectedIndex = 0;
            redraw();
            updateTitlebar();
        }

        private void updateTitlebar(string text = "")
        {
            if (_currentFile == "") text = "Untitled";
            if (text == "") text = Path.GetFileName(_currentFile);
            Text = $"{(_saved ? "" : "*")}{text} - Coach Draw v{Application.ProductVersion}";
            saveToolStripMenuItem.Enabled = _currentFile != "";
        }

        void selectObj(object sender, EventArgs e)
        {
            var mn = (ToolStripMenuItem)sender;
            if ((int)mn.Tag == -1) return;
            _selected = _objs[(int)mn.Tag];
            redraw();
        }

        void deselectObj(object sender, ToolStripDropDownClosedEventArgs e)
        {
            _selected = null;
            redraw();
        }

        #region Panel Events
        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _mouseDown = true;
            _startPoint.X = e.X;
            _startPoint.Y = e.Y;
            if (_selectedTool == "Pencil")
            {
                _tempcoords.Clear();
                _tempcoords.Add(new Point(_startPoint.X, _startPoint.Y));
            }
            _tempDraw = (Bitmap)_snapshot.Clone();
        }

        // TODO: Fix ugly
        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            _selected = null;
            if (e.Button == MouseButtons.Right)
            {

                List<int> hits = new List<int>();
                for (int i = 0; i < _objs.Count; i++)
                {
                    if (_objs[i].hitBox != null && _objs[i].hitBox.IsVisible(e.X, e.Y) ||
                        _objs[i].objLine != null && _objs[i].objLine.hitBox != null && _objs[i].objLine.hitBox.IsVisible(e.X, e.Y))
                    {
                        hits.Add(i);
                    }
                }
                var mn = new ContextMenuStrip();
                mn.Closed += deselectObj;
                if (hits.Count == 1)
                {
                    mn.Items.AddRange(buildMenu((int)_objs[hits[0]].objType,
                        _objs[hits[0]].objLine == null ? -1 : (int)_objs[hits[0]].objLine.lineType,
                        _objs[hits[0]].objLine == null ? -1 : (int)_objs[hits[0]].objLine.endType));
                    mn.Tag = hits[0];
                    _selected = _objs[hits[0]];
                    redraw();
                }
                else if (hits.Count > 1)
                {
                    var hitsMenu = new List<ToolStripMenuItem>();
                    foreach (var hit in hits)
                    {
                        var newMn = new ToolStripMenuItem(hit.ToString());
                        newMn.DropDownItems.AddRange(buildMenu((int)_objs[hit].objType,
                            _objs[hit].objLine == null ? -1 : (int)_objs[hit].objLine.lineType,
                            _objs[hit].objLine == null ? -1 : (int)_objs[hit].objLine.endType));
                        newMn.Tag = hit;
                        newMn.MouseHover += selectObj;
                        hitsMenu.Add(newMn);
                    }
                    mn.Items.AddRange(hitsMenu.ToArray<ToolStripItem>());
                    mn.Tag = -1;
                }
                if (mn.Items.Count > 0) mn.Show(panel1, new Point(e.X, e.Y));
            }
            else if (e.Button == MouseButtons.Left)
            {
                if (!_mouseDown) return;
                _mouseDown = false;
                foreach (DrawObj o in _objs)
                {
                    if (o.objLoc.Equals(_startPoint))
                        return;
                }
                _endPoint.X = Math.Min(e.X, panel1.Width);
                _endPoint.Y = Math.Min(e.Y, panel1.Height);
                var newObj = new DrawObj
                {
                    objType = (ItemType)Enum.Parse(typeof(ItemType), ItemTypeBox.Text.Replace(" ", "").Replace(".", "")),
                    objLoc = _startPoint,
                    color = colorDialog1.Color,
                    objLabel = PlayerNumBox.Text == "None" ? -1 : int.Parse(PlayerNumBox.Text)
                };
                if (!_startPoint.Equals(_endPoint) || _tempcoords.Count > 0)
                {
                    newObj.objLine = new Line
                    {
                        lineType = (LineType)Enum.Parse(typeof(LineType), LineTypeBox.Text.Replace(" ", "")),
                        endType = (EndType)Enum.Parse(typeof(EndType), EndTypeBox.Text.Replace(" ", "")),
                        color = colorDialog1.Color
                    };
                    foreach (ToolStripMenuItem item in widthBox.DropDownItems)
                        if (item.Checked) newObj.objLine.lineWidth = byte.Parse(item.Text.Remove(1));

                    int lineLength = 0;
                    if (_selectedTool == "Line")
                    {
                        lineLength = Smoothing.GetLineLength(_startPoint, _endPoint);
                        newObj.objLine.points.Add(_startPoint);
                        newObj.objLine.points.Add(_endPoint);
                    }
                    else if (_selectedTool == "Pencil")
                    {
                        newObj.objLine.points = _tempcoords.ToList();
                        lineLength = newObj.objLine.getAggregateLength(0, newObj.objLine.points.Count - 1);
                    }
                    newObj.objLine.cleanDuplicates();
                    if (lineLength < 20 || newObj.objLine.points.Count < 2) newObj.objLine = null;
                }
                _objs.Add(newObj);
                _saved = false;
                updateTitlebar();
            }
            redraw();
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_mouseDown) return;
            _endPoint.X = Math.Min(e.X, panel1.Width);
            _endPoint.Y = Math.Min(e.Y, panel1.Height);
            panel1.Invalidate();
            panel1.Update();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            if (_tempDraw != null && _mouseDown)
            {
                Graphics g = Graphics.FromImage(_tempDraw);
                Pen myPen = new Pen(Color.Black, 1);
                if (_selectedTool == "Line")
                    g.DrawLine(myPen, _startPoint, _endPoint);
                else if (_selectedTool == "Pencil")
                {
                    _tempcoords.Add(_endPoint);
                    g.DrawLines(myPen, _tempcoords.ToArray());
                }
                myPen.Dispose();
                e.Graphics.DrawImageUnscaled(_tempDraw, 0, 0);
                g.Dispose();
                _tempDraw = (Bitmap)_snapshot.Clone();
            }
            else if (_snapshot != null)
                e.Graphics.DrawImageUnscaled(_snapshot, 0, 0);
        }
        #endregion

        private void clickItemList(object sender, EventArgs e)
        {
            ToolStripMenuItem mn = (ToolStripMenuItem)sender;
            int i = -1;
            ToolStripItem next = mn.OwnerItem;
            while (next != null)
            {
                if (next.Owner.GetType() == typeof(ContextMenuStrip))
                {
                    i = (int)next.Owner.Tag == -1 ? (int)next.Tag : (int)next.Owner.Tag;
                    break;
                }
                next = next.OwnerItem;
            }
            switch ((int)mn.OwnerItem.Tag)
            {
                case 0:
                    _objs[i].objType = (ItemType)Enum.Parse(typeof(ItemType), mn.Text);
                    break;
                case 1:
                    if (mn.Text == "Delete Line")
                    {
                        if (MessageBox.Show("Deleting this line cannot be undone. Are you sure you want to continue?", "Delete Line", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                            _objs[i].objLine = null;
                    }
                    else
                        _objs[i].objLine.lineType = (LineType)Enum.Parse(typeof(LineType), mn.Text);
                    break;
                case 2:
                    _objs[i].objLine.endType = (EndType)Enum.Parse(typeof(EndType), mn.Text);
                    break;
            }
            _saved = false;
            _selected = null;
            redraw();
            updateTitlebar();
        }

        private void deleteItem(object sender, EventArgs e)
        {
            var mn = (ToolStripMenuItem)sender;
            int i = mn.Owner.GetType() == typeof(ContextMenuStrip) ? (int)mn.Owner.Tag : (int)mn.OwnerItem.Tag;
            _objs.RemoveAt(i);
            _saved = false;
            redraw();
            updateTitlebar();
        }

        // TODO: Review
        private ToolStripItem[] buildMenu(int it, int lt, int et)
        {
            var itemList = new List<ToolStripItem>();
            var lineList = new List<ToolStripItem>();
            var endList = new List<ToolStripItem>();
            foreach (string itemtype in Enum.GetNames(typeof(ItemType)))
            {
                var m = new ToolStripMenuItem(itemtype, null, clickItemList);
                if (it != -1 && Enum.GetName(typeof(ItemType), it).Equals(itemtype)) { m.Checked = true; }
                itemList.Add(m);
            }
            lineList.Add(new ToolStripMenuItem("Delete Line", null, clickItemList));
            lineList.Add(new ToolStripSeparator());
            foreach (string linetype in Enum.GetNames(typeof(LineType)))
            {
                var m = new ToolStripMenuItem(linetype, null, clickItemList);
                if (lt != -1 && !m.Checked && Enum.GetName(typeof(LineType), lt).Equals(linetype)) { m.Checked = true; }
                lineList.Add(m);
            }
            foreach (string endtype in Enum.GetNames(typeof(EndType)))
            {
                var m = new ToolStripMenuItem(endtype, null, clickItemList);
                if (et != -1 && !m.Checked && Enum.GetName(typeof(EndType), et).Equals(endtype)) { m.Checked = true; }
                endList.Add(m);
            }
            ToolStripItem[] mn = new ToolStripItem[5];
            mn[0] = new ToolStripMenuItem("Item Type", null, itemList.ToArray()) { Tag = 0 };
            mn[1] = new ToolStripMenuItem("Line Type", null, lineList.ToArray()) { Tag = 1 };
            if (lt == -1) mn[1].Enabled = false;
            mn[2] = new ToolStripMenuItem("End Type", null, endList.ToArray()) { Tag = 2 };
            if (et == -1) mn[2].Enabled = false;
            mn[3] = new ToolStripSeparator();
            mn[4] = new ToolStripMenuItem("Delete", null, deleteItem);
            return mn;
        }

        private void redraw()
        {
            _snapshot.Dispose();
            _snapshot = new Bitmap(panel1.ClientRectangle.Width, panel1.ClientRectangle.Height);
            using (Graphics g = Graphics.FromImage(_snapshot))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                drawRink(g);
                g.SmoothingMode = SmoothingMode.None;
                foreach (DrawObj o in _objs)
                {
                    o.Draw(g, _selected?.Equals(o) ?? false);
                }
            }
            panel1.Invalidate();
            panel1.Update();
        }

        // TODO: Fix this ugly mess
        [SuppressMessage("ReSharper", "ArrangeRedundantParentheses")]
        private void drawRink(Graphics g)
        {
            using (var blackPen = new Pen(Color.Black, 2))
            using (var redPen = new Pen(Color.Red, 2))
            using (var bluePen = new Pen(Color.Blue, 2))
            using (var redBrush = new SolidBrush(Color.Red))
            using (var blueBrush = new SolidBrush(Color.Blue))
            {
                /**  Lines  **/
                g.DrawLine(bluePen, (RinkWidth / 2) - curSpecs.BlueLineFromCenter, 0, (RinkWidth / 2) - curSpecs.BlueLineFromCenter, RinkHeight); //Left blue line
                g.DrawLine(bluePen, (RinkWidth / 2) + curSpecs.BlueLineFromCenter, 0, (RinkWidth / 2) + curSpecs.BlueLineFromCenter, RinkHeight); //Right blue line
                Pen centerPen = new Pen(Color.Red, 5)
                {
                    DashCap = DashCap.Round,
                    DashStyle = DashStyle.Dash,
                    DashOffset = 1
                };
                g.DrawLine(centerPen, (RinkWidth / 2), 0, (RinkWidth / 2), RinkHeight); //Centerline
                centerPen.Dispose();
                g.DrawLine(redPen, 65, 8, 65, RinkHeight - 8); //Left red line
                g.DrawLine(redPen, RinkWidth - 66, 8, RinkWidth - 66, RinkHeight - 8); //Right red line
                                                                                       /**  Nets  **/
                g.DrawLine(blackPen, 65 - 20, (RinkHeight / 2) - 15, 64, (RinkHeight / 2) - 15); //Left top
                g.DrawLine(blackPen, 65 - 20, (RinkHeight / 2) + 15, 64, (RinkHeight / 2) + 15); //Left bottom
                g.DrawLine(blackPen, 65 - 20, (RinkHeight / 2) - 15, 65 - 20, (RinkHeight / 2) + 15); //Left side
                g.FillPie(Brushes.PaleTurquoise, 66 - 40, (RinkHeight / 2) - 40, 80, 80, 270, 180); //Left arc

                g.DrawLine(blackPen, RinkWidth - 65 + 20, (RinkHeight / 2) - 15, RinkWidth - 65, (RinkHeight / 2) - 15); //Right
                g.DrawLine(blackPen, RinkWidth - 65 + 20, (RinkHeight / 2) + 15, RinkWidth - 65, (RinkHeight / 2) + 15); //Right
                g.DrawLine(blackPen, RinkWidth - 65 + 20, (RinkHeight / 2) - 15, RinkWidth - 65 + 20, (RinkHeight / 2) + 15); //Right
                g.FillPie(Brushes.PaleTurquoise, RinkWidth - 67 - 40, (RinkHeight / 2) - 40, 80, 80, 90, 180); //Right arc
                                                                                                               /**  Black border  **/
                g.DrawLine(blackPen, 100, 0, RinkWidth - 100, 0); //Top black line
                g.DrawLine(blackPen, 1, 100, 1, RinkHeight - 100); //Left black line
                g.DrawLine(blackPen, RinkWidth - 1, 100, RinkWidth - 1, RinkHeight - 100); //Right black line
                g.DrawLine(blackPen, 100, RinkHeight - 1, RinkWidth - 100, RinkHeight - 1); //Bottom black line
                g.DrawArc(blackPen, 0, 0, 200, 200, 180, 90); //Top left curve
                g.DrawArc(blackPen, 0, RinkHeight - 201, 200, 200, 180, -90); //Bottom left curve
                g.DrawArc(blackPen, RinkWidth - 201, 0, 200, 200, 270, 90); //Top right curve
                g.DrawArc(blackPen, RinkWidth - 201, RinkHeight - 201, 200, 200, 90, -90); //Bottom right curve
                                                                                           /**  Dots  **/
                g.FillEllipse(blueBrush, (RinkWidth / 2) - curSpecs.DotRadius, (RinkHeight / 2) - curSpecs.DotRadius, curSpecs.DotRadius * 2, curSpecs.DotRadius * 2); //Center dot
                g.FillEllipse(redBrush, curSpecs.EdgeToCircle - curSpecs.DotRadius, (RinkHeight / 2) - curSpecs.CenterNetToCircle - curSpecs.DotRadius, curSpecs.DotRadius * 2, curSpecs.DotRadius * 2); //Top left
                g.FillEllipse(redBrush, RinkWidth - curSpecs.EdgeToCircle - curSpecs.DotRadius, (RinkHeight / 2) - curSpecs.CenterNetToCircle - curSpecs.DotRadius, curSpecs.DotRadius * 2, curSpecs.DotRadius * 2); //Top right circle
                g.FillEllipse(redBrush, curSpecs.EdgeToCircle - curSpecs.DotRadius, (RinkHeight / 2) + curSpecs.CenterNetToCircle - curSpecs.DotRadius, curSpecs.DotRadius * 2, curSpecs.DotRadius * 2); //Bottom left circle
                g.FillEllipse(redBrush, RinkWidth - curSpecs.EdgeToCircle - curSpecs.DotRadius, (RinkHeight / 2) + curSpecs.CenterNetToCircle - curSpecs.DotRadius, curSpecs.DotRadius * 2, curSpecs.DotRadius * 2); //Bottom right circle
                g.FillEllipse(redBrush, (RinkWidth / 2) - curSpecs.AuxDotsFromCenter - curSpecs.DotRadius, (RinkHeight / 2) - curSpecs.CenterNetToCircle - curSpecs.DotRadius, curSpecs.DotRadius * 2, curSpecs.DotRadius * 2);
                g.FillEllipse(redBrush, (RinkWidth / 2) + curSpecs.AuxDotsFromCenter - curSpecs.DotRadius, (RinkHeight / 2) - curSpecs.CenterNetToCircle - curSpecs.DotRadius, curSpecs.DotRadius * 2, curSpecs.DotRadius * 2);
                g.FillEllipse(redBrush, (RinkWidth / 2) - curSpecs.AuxDotsFromCenter - curSpecs.DotRadius, (RinkHeight / 2) + curSpecs.CenterNetToCircle - curSpecs.DotRadius, curSpecs.DotRadius * 2, curSpecs.DotRadius * 2);
                g.FillEllipse(redBrush, (RinkWidth / 2) + curSpecs.AuxDotsFromCenter - curSpecs.DotRadius, (RinkHeight / 2) + curSpecs.CenterNetToCircle - curSpecs.DotRadius, curSpecs.DotRadius * 2, curSpecs.DotRadius * 2);

                /**  Circles  **/
                g.DrawEllipse(redPen, curSpecs.EdgeToCircle - curSpecs.CircleRadius, (RinkHeight / 2) - curSpecs.CenterNetToCircle - curSpecs.CircleRadius, curSpecs.CircleRadius * 2, curSpecs.CircleRadius * 2); //Top left
                g.DrawEllipse(redPen, RinkWidth - curSpecs.EdgeToCircle - curSpecs.CircleRadius, (RinkHeight / 2) - curSpecs.CenterNetToCircle - curSpecs.CircleRadius, curSpecs.CircleRadius * 2, curSpecs.CircleRadius * 2); //Top right
                g.DrawEllipse(redPen, curSpecs.EdgeToCircle - curSpecs.CircleRadius, (RinkHeight / 2) + curSpecs.CenterNetToCircle - curSpecs.CircleRadius, curSpecs.CircleRadius * 2, curSpecs.CircleRadius * 2); //Bottom left
                g.DrawEllipse(redPen, RinkWidth - curSpecs.EdgeToCircle - curSpecs.CircleRadius, (RinkHeight / 2) + curSpecs.CenterNetToCircle - curSpecs.CircleRadius, curSpecs.CircleRadius * 2, curSpecs.CircleRadius * 2); //Bottom right
                g.DrawEllipse(bluePen, (RinkWidth / 2) - curSpecs.CircleRadius, (RinkHeight / 2) - curSpecs.CircleRadius, curSpecs.CircleRadius * 2, curSpecs.CircleRadius * 2);
            }
        }

        #region Menu Bar
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (!(sender is ToolStripButton btnClicked)) return;
            Line.Checked = false;
            Pencil.Checked = false;
            btnClicked.Checked = true;
            _selectedTool = btnClicked.Name;
        }

        private void colorLabel_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                if (colorDialog1.Color.IsNamedColor) colorLabel.Text = colorDialog1.Color.Name;
                else
                {
                    int x = colorDialog1.Color.ToArgb();
                    colorLabel.Text = "#" + ((x >> 16) & 0xFF).ToString("X2") + ((x >> 8) & 0xFF).ToString("X2") + (x & 0xFF).ToString("X2");
                }
                colorLabel.ForeColor = colorDialog1.Color;
            }
        }

        private void ptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem t in widthBox.DropDownItems)
                t.Checked = false;
            ((ToolStripMenuItem)sender).Checked = true;
            widthBox.Text = "Line Width (" + ((ToolStripMenuItem)sender).Text.Remove(1) + ")";
        }

        private void ItemTypeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ItemTypeBox.Text == "Player Number") PlayerNumBox.SelectedIndex = 1;
            else if(PlayerNumBox.Items.Count > 0) PlayerNumBox.SelectedIndex = 0;
        }

        private void LineTypeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(LineTypeBox.Text == "Shot" || LineTypeBox.Text == "Pass" || LineTypeBox.Text == "Lateral")
            {
                ItemTypeBox.SelectedIndex = 9;
                EndTypeBox.SelectedIndex = 0;
                PlayerNumBox.SelectedIndex = 0;
                toolStripButton1_Click(Line, null);
                Pencil.Enabled = false;
            }
            if (LineTypeBox.Text == "Shot")
            {
                EndTypeBox.SelectedIndex = 1;
                EndTypeBox.Enabled = false;
            }
            else if (LineTypeBox.Text == "Lateral")
            {
                EndTypeBox.Enabled = true;
                Pencil.Enabled = false;
            }
            else
            {
                EndTypeBox.Enabled = true;
                Pencil.Enabled = true;
            }

        }
        #endregion

        #region Recent Files
        private void addRecentFile(string path)
        {
            var sc = Properties.Settings.Default.recentFiles ?? new StringCollection();
            if (sc.Contains(path)) return;
            sc.Add(path);
            if (sc.Count > 5) sc.RemoveAt(0);
            updateRecentFiles();
        }

        private void updateRecentFiles()
        {
            var sc = Properties.Settings.Default.recentFiles;
            if (sc != null && sc.Count > 0)
            {
                for (int i = 0; i < sc.Count; i++)
                {
                    var path = sc[i];
                    if (!File.Exists(path))
                    {
                        sc.RemoveAt(i--);
                        continue;
                    }
                    var tempItem = new ToolStripMenuItem(compactString(path, 50), null, clickRecentFile) { Tag = path };
                    recentFilesToolStripMenuItem.DropDownItems.Add(tempItem);
                }
                Properties.Settings.Default.recentFiles = sc;
                Properties.Settings.Default.Save();
                recentFilesToolStripMenuItem.Enabled = sc.Count > 0;
            }
            else
                recentFilesToolStripMenuItem.Enabled = false;
        }

        private void clickRecentFile(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            if (item.Tag != null)
                openFile((string)item.Tag, false);
        }
        #endregion

        #region Toolbar/saving/opening stuff
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!checkSaved("starting a new play")) return;
            txtPlayName.Text = "";
            txtPlayDesc.Text = "";
            _currentFile = "";
            _saved = true;
            _objs.Clear();
            redraw();
            updateTitlebar("Untitled");
        }

        private void openFile(string path, bool skipSaveCheck)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show("Sorry, that file no longer exists!", "File Not Found");
                return;
            }
            if (!skipSaveCheck && !checkSaved("opening a new play")) return;
            _objs.Clear();
            redraw();
            PlayInfo result = null;
            if (path.ToUpper().EndsWith(".PLY"))
                result = Plays.LoadPLYFile(path, ref _objs);
            else if (path.ToUpper().EndsWith(".PLYX"))
            {
                result = Plays.LoadPLYXFile(path, ref _objs);
                _currentFile = path;
                _saved = true;
            }
            if (result == null)
            {
                MessageBox.Show("Something weird happened...");
            }
            else
            {
                txtPlayName.Text = result.Name;
                txtPlayDesc.Text = result.Desc;
                addRecentFile(path);
                redraw();
                updateTitlebar(Path.GetFileName(path));
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!checkSaved("opening a new play")) return;
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "All supported files (*.ply, *.plyx)|*.ply;*.plyx|New play files (*.plyx)|*.plyx|HockeyVision play files (*.ply)|*.ply|All files (*.*)|*.*",
                FilterIndex = 2
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                openFile(dlg.FileName, true);
        }

        private bool checkSaved(string operation)
        {
            if (_saved) return true;
            DialogResult d = MessageBox.Show("The current play is not saved. Do you want to save before " + operation + "?", "Unsaved Play", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button3);
            if (d == DialogResult.Yes)
                saveToolStripMenuItem_Click(saveToolStripMenuItem, null);
            else if (d == DialogResult.Cancel)
                return false;
            return true;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_currentFile == "") return;
            if (!Plays.savePLYXFile(_currentFile, _objs, txtPlayName.Text, txtPlayDesc.Text, plyxVersion)) return;
            _saved = true;
            updateTitlebar();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (checkSaved("exiting"))
                Application.Exit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!checkSaved("exiting"))
                e.Cancel = true;
        }
        #endregion

        private void openHVPlayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (frmManage ld = new frmManage())
            {
                if (ld.ShowDialog() == DialogResult.Yes && ld.openPlay != "")
                    openFile(ld.openPlay, false);
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (frmSaveAs ld = new frmSaveAs(txtPlayName.Text))
            {
                if (ld.ShowDialog() == DialogResult.Yes && Plays.savePLYXFile(ld.fileName, _objs, ld.playName, txtPlayDesc.Text, plyxVersion))
                {
                    txtPlayName.Text = ld.playName;
                    _saved = true;
                    _currentFile = ld.fileName;
                    addRecentFile(ld.fileName);
                    updateTitlebar();
                }
            }
        }

        private Font ShrinkFont(Graphics g, string text, Font font, Size proposedSize)
        {
            while (font.Size > 0)
            {
                SizeF size = g.MeasureString(text, font);
                if (size.Height <= proposedSize.Height && size.Width <= proposedSize.Width)
                    return font;

                Font oldFont = font;
                font = new Font(font.Name, font.Size * 0.9f, font.Style);
                oldFont.Dispose();
            }
            // Totally impossible to create a font small enough, return null and probably crash for now
            return null;
        }

        private void printCurrent_Click(object sender, EventArgs e)
        {
            var pd = new PrintDocument {DefaultPageSettings = {Landscape = true}};
            pd.PrintPage += (printSender, pe) =>
            {
                float posy = 50;
                var nameFont = new Font("Helvetica", 14.0f, FontStyle.Bold);
                var nameSize = pe.Graphics.MeasureString(txtPlayName.Text, nameFont);
                pe.Graphics.DrawString(txtPlayName.Text, nameFont, new SolidBrush(Color.Black), new RectangleF(50.0f, posy, 900.0f, nameSize.Height), new StringFormat { Alignment = StringAlignment.Center });
                posy += nameSize.Height;
                pe.Graphics.DrawImage(_snapshot, new Rectangle(50, (int)posy, pe.PageSettings.PaperSize.Height - 100, Convert.ToInt32(_snapshot.Height * ((pe.PageSettings.PaperSize.Height - 100.0) / _snapshot.Width))));
                posy += _snapshot.Height;
                var descFont = ShrinkFont(pe.Graphics, txtPlayDesc.Text, new Font("Helvetica", 10.0f), new Size(900, 750 - (int)posy));
                var descSize = pe.Graphics.MeasureString(txtPlayDesc.Text, descFont);
                pe.Graphics.DrawString(txtPlayDesc.Text, descFont, new SolidBrush(Color.Black), new RectangleF(50.0f, posy, 900.0f, 750.0f - posy));
                pe.Graphics.DrawString("CoachDraw © 2017", new Font("Helvetica", 5.0f), new SolidBrush(Color.Black), 50, 825.0f);
            };
            var dialog = new PrintDialog { Document = pd };
            if (dialog.ShowDialog() != DialogResult.OK) return;
            PrintPreviewDialog pp = new PrintPreviewDialog { Document = pd };
            pp.ShowDialog();
            //pd.PrinterSettings = dialog.PrinterSettings;
            //pd.Print();
        }

        private void printMultiple_Click(object sender, EventArgs e)
        {
            using (var mp = new frmMultiPrint(_currentFile, _lastSelectedMultiPrint, _lastSetMultiPrint))
            {
                if (mp.ShowDialog() == DialogResult.Yes)
                    PrintMultiplePlays(mp.Plays);
                _lastSelectedMultiPrint = mp.LastSelected;
                _lastSetMultiPrint = mp.Plays ?? new List<string> { "", "", "", "" };
            }
        }

        private void PrintMultiplePlays(List<string> plays)
        {
            var pd = new PrintDocument();
            pd.PrintPage += (printSender, pe) =>
            {
                float posy = 50;
                foreach (string play in plays)
                {
                    if (play == "")
                    {
                        posy += 250.0f;
                        continue;
                    }
                    var tempObjs = new List<DrawObj>();
                    var result = Plays.LoadPLYXFile(play, ref tempObjs);
                    var rink = new Bitmap(1000, 500);
                    using (var g = Graphics.FromImage(rink))
                    {
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        drawRink(g);
                        g.SmoothingMode = SmoothingMode.None;
                        foreach (var o in tempObjs)
                        {
                            o.Draw(g, false);
                        }
                    }
                    pe.Graphics.DrawLine(new Pen(Color.Black, 1.0f), 0.0f, posy, 1100.0f, posy);
                    posy += 5.0f;
                    int newWidth = (int)(240.0 / rink.Height * rink.Width);
                    pe.Graphics.DrawImage(rink, new Rectangle(50, (int)posy, newWidth, 240));
                    Font nameFont = ShrinkFont(pe.Graphics, result.Name, new Font("Helvetica", 11.0f, FontStyle.Bold), new Size(750 - newWidth - 10, 100));
                    SizeF nameSize = pe.Graphics.MeasureString(result.Name, nameFont);
                    pe.Graphics.DrawString(result.Name, nameFont, new SolidBrush(Color.Black), 50.0f + newWidth + 10.0f, posy);
                    Font descFont = ShrinkFont(pe.Graphics, result.Desc, new Font("Helvetica", 8.0f), new Size(750 - newWidth - 10, 240 - (int)nameSize.Height - 5));
                    pe.Graphics.DrawString(result.Desc, descFont, new SolidBrush(Color.Black), 50.0f + newWidth + 10.0f, posy + nameSize.Height + 5.0f);
                    posy += 245.0f;
                    pe.Graphics.DrawLine(new Pen(Color.Black, 1.0f), 0.0f, posy, 1100.0f, posy);
                    rink.Dispose();
                }
                posy += 25.0f;
                pe.Graphics.DrawString("CoachDraw © 2017", new Font("Helvetica", 5.0f), new SolidBrush(Color.Black), 50, posy);
            };
            var dialog = new PrintDialog { Document = pd };
            if (dialog.ShowDialog() != DialogResult.OK) return;
            var pp = new PrintPreviewDialog
            {
                Document = pd,
                Height = Height,
                Width = Width
            };
            pp.ShowDialog();
            //pd.PrinterSettings = dialog.PrinterSettings;
            //pd.Print();
        }
    }

    public class RinkSpecs
    {
        //Distances in feet
        public int BlueLineFromCenter = 30;
        public int AuxDotsFromCenter = 22;
        public int RedToBlue = 60;
        public int EdgeToRed = 12;
        public int CenterNetToCircle = 22;
        public int RedToCircle = 20;
        public int EdgeToCircle = 32;
        public int NetWidth = 4;
        public int NetLength = 6;
        public int NetArcRadius = 8;
        public int CircleRadius = 15;
        public float DotRadius = 2.5F;
        public int HashMarkSize = 4;
        private int _curScale = 1;

        public RinkSpecs(int newScale)
        {
            SetScale(newScale);
        }

        public void SetScale(int newScale)
        {
            BlueLineFromCenter /= _curScale; BlueLineFromCenter *= newScale;
            AuxDotsFromCenter /= _curScale; AuxDotsFromCenter *= newScale;
            RedToBlue /= _curScale; RedToBlue *= newScale;
            EdgeToRed /= _curScale; EdgeToRed *= newScale;
            CenterNetToCircle /= _curScale; CenterNetToCircle *= newScale;
            RedToCircle /= _curScale; RedToCircle *= newScale;
            EdgeToCircle /= _curScale; EdgeToCircle *= newScale;
            NetWidth /= _curScale; NetWidth *= newScale;
            NetLength /= _curScale; NetLength *= newScale;
            NetArcRadius /= _curScale; NetArcRadius *= newScale;
            CircleRadius /= _curScale; CircleRadius *= newScale;
            _curScale = newScale;
        }
    }
}
