using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
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
            var sb = new StringBuilder(length + 1);
            PathCompactPathEx(sb, path, length + 1, 0);
            return sb.ToString();
        }
        /******************************************/
        private const double _requiredScale = 5;
        private Point _startPoint;
        private Point _endPoint;
        private bool _mouseDown;
        private Bitmap _snapshot;
        private Bitmap _tempDraw;
        private readonly List<Point> _tempcoords;
        private Play _currentPlay;
        private RinkSpecs _curSpecs = new IIHFRink(_requiredScale); //Scale
        private string _selectedTool = "Line";
        private string _currentFile = "";
        private bool _saved = true;
        private DrawObj _selected;
        private string _lastSelectedMultiPrint = "";
        private List<string> _lastSetMultiPrint = new List<string> { "", "", "", "" };

        public frmMain()
        {
            InitializeComponent();
            _snapshot = new Bitmap(panel1.ClientRectangle.Width, ClientRectangle.Height);
            _tempDraw = (Bitmap)_snapshot.Clone();
            _tempcoords = new List<Point>();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            _currentPlay = new Play();
            updateRecentFiles();
            ItemTypeBox.SelectedIndex = 0;
            LineTypeBox.SelectedIndex = 0;
            EndTypeBox.SelectedIndex = 0;
            PlayerNumBox.Items.Add("None");
            for (var i = 0; i < 100; i++)
                PlayerNumBox.Items.Add(i.ToString());
            PlayerNumBox.SelectedIndex = 0;
            foreach (var val in Enum.GetNames(typeof(RinkType)))
                RinkTypeBox.Items.Add(val);
            RinkTypeBox.SelectedIndex = 0;
            // Only start monitoring selected type after default value is set to prevent implicit redraw
            RinkTypeBox.SelectedIndexChanged += RinkTypeBox_SelectedIndexChanged;
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
            _selected = _currentPlay.Objects[(int)mn.Tag];
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

                var hits = new List<int>();
                for (var i = 0; i < _currentPlay.Objects.Count; i++)
                {
                    if (_currentPlay.Objects[i].hitBox != null && _currentPlay.Objects[i].hitBox.IsVisible(e.X, e.Y) ||
                        _currentPlay.Objects[i].objLine != null && _currentPlay.Objects[i].objLine.hitBox != null && _currentPlay.Objects[i].objLine.hitBox.IsVisible(e.X, e.Y))
                    {
                        hits.Add(i);
                    }
                }
                var mn = new ContextMenuStrip();
                mn.Closed += deselectObj;
                if (hits.Count == 1)
                {
                    mn.Items.AddRange(buildMenu((int)_currentPlay.Objects[hits[0]].objType,
                        _currentPlay.Objects[hits[0]].objLine == null ? -1 : (int)_currentPlay.Objects[hits[0]].objLine.lineType,
                        _currentPlay.Objects[hits[0]].objLine == null ? -1 : (int)_currentPlay.Objects[hits[0]].objLine.endType));
                    mn.Tag = hits[0];
                    _selected = _currentPlay.Objects[hits[0]];
                    redraw();
                }
                else if (hits.Count > 1)
                {
                    var hitsMenu = new List<ToolStripMenuItem>();
                    foreach (var hit in hits)
                    {
                        var newMn = new ToolStripMenuItem(hit.ToString());
                        newMn.DropDownItems.AddRange(buildMenu((int)_currentPlay.Objects[hit].objType,
                            _currentPlay.Objects[hit].objLine == null ? -1 : (int)_currentPlay.Objects[hit].objLine.lineType,
                            _currentPlay.Objects[hit].objLine == null ? -1 : (int)_currentPlay.Objects[hit].objLine.endType));
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
                foreach (var o in _currentPlay.Objects)
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

                    var lineLength = 0;
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
                _currentPlay.Objects.Add(newObj);
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
                var g = Graphics.FromImage(_tempDraw);
                var myPen = new Pen(Color.Black, 1);
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
            var mn = (ToolStripMenuItem)sender;
            var i = -1;
            var next = mn.OwnerItem;
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
                    _currentPlay.Objects[i].objType = (ItemType)Enum.Parse(typeof(ItemType), mn.Text);
                    break;
                case 1:
                    if (mn.Text == "Delete Line")
                    {
                        if (MessageBox.Show("Deleting this line cannot be undone. Are you sure you want to continue?", "Delete Line", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                            _currentPlay.Objects[i].objLine = null;
                    }
                    else
                        _currentPlay.Objects[i].objLine.lineType = (LineType)Enum.Parse(typeof(LineType), mn.Text);
                    break;
                case 2:
                    _currentPlay.Objects[i].objLine.endType = (EndType)Enum.Parse(typeof(EndType), mn.Text);
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
            var i = mn.Owner.GetType() == typeof(ContextMenuStrip) ? (int)mn.Owner.Tag : (int)mn.OwnerItem.Tag;
            _currentPlay.Objects.RemoveAt(i);
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
            foreach (var itemtype in Enum.GetNames(typeof(ItemType)))
            {
                var m = new ToolStripMenuItem(itemtype, null, clickItemList);
                if (it != -1 && Enum.GetName(typeof(ItemType), it).Equals(itemtype)) { m.Checked = true; }
                itemList.Add(m);
            }
            lineList.Add(new ToolStripMenuItem("Delete Line", null, clickItemList));
            lineList.Add(new ToolStripSeparator());
            foreach (var linetype in Enum.GetNames(typeof(LineType)))
            {
                var m = new ToolStripMenuItem(linetype, null, clickItemList);
                if (lt != -1 && !m.Checked && Enum.GetName(typeof(LineType), lt).Equals(linetype)) { m.Checked = true; }
                lineList.Add(m);
            }
            foreach (var endtype in Enum.GetNames(typeof(EndType)))
            {
                var m = new ToolStripMenuItem(endtype, null, clickItemList);
                if (et != -1 && !m.Checked && Enum.GetName(typeof(EndType), et).Equals(endtype)) { m.Checked = true; }
                endList.Add(m);
            }
            var mn = new ToolStripItem[5];
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
            using (var g = Graphics.FromImage(_snapshot))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                drawRink(g);
                g.SmoothingMode = SmoothingMode.None;
                foreach (var o in _currentPlay.Objects)
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
            var rinkCenterX = (float)_curSpecs.RinkWidth / 2f;
            var rinkCenterY = (float)_curSpecs.RinkHeight / 2f;
            /**  Lines  **/
            g.DrawLine(bluePen, rinkCenterX - (float)_curSpecs.BlueLineFromCenter, 0, rinkCenterX - (float)_curSpecs.BlueLineFromCenter, (float)_curSpecs.RinkHeight); //Left blue line
            g.DrawLine(bluePen, rinkCenterX + (float)_curSpecs.BlueLineFromCenter, 0, rinkCenterX + (float)_curSpecs.BlueLineFromCenter, (float)_curSpecs.RinkHeight); //Right blue line
            var centerPen = new Pen(Color.Red, 5)
            {
                DashCap = DashCap.Round,
                DashStyle = DashStyle.Dash,
                DashOffset = 0.5f
            };
            g.DrawLine(centerPen, rinkCenterX, 0, rinkCenterX, (float)_curSpecs.RinkHeight); //Centerline
            centerPen.Dispose();
            g.DrawLine(redPen, 65, 8, 65, (float)_curSpecs.RinkHeight - 8); //Left red line
            g.DrawLine(redPen, (float)_curSpecs.RinkWidth - 66, 8, (float)_curSpecs.RinkWidth - 66, (float)_curSpecs.RinkHeight - 8); //Right red line

            /**  Nets  **/
            g.DrawLine(blackPen, 65 - 20, rinkCenterY - 15, 64, rinkCenterY - 15); //Left top
            g.DrawLine(blackPen, 65 - 20, rinkCenterY + 15, 64, rinkCenterY + 15); //Left bottom
            g.DrawLine(blackPen, 65 - 20, rinkCenterY - 15, 65 - 20, rinkCenterY + 15); //Left side
            g.FillPie(Brushes.PaleTurquoise, 66 - 40, rinkCenterY - 40, 80, 80, 270, 180); //Left arc

            g.DrawLine(blackPen, (float)_curSpecs.RinkWidth - 65 + 20, rinkCenterY - 15, (float)_curSpecs.RinkWidth - 65, rinkCenterY - 15); //Right
            g.DrawLine(blackPen, (float)_curSpecs.RinkWidth - 65 + 20, rinkCenterY + 15, (float)_curSpecs.RinkWidth - 65, rinkCenterY + 15); //Right
            g.DrawLine(blackPen, (float)_curSpecs.RinkWidth - 65 + 20, rinkCenterY - 15, (float)_curSpecs.RinkWidth - 65 + 20, rinkCenterY + 15); //Right
            g.FillPie(Brushes.PaleTurquoise, (float)_curSpecs.RinkWidth - 67 - 40, rinkCenterY - 40, 80, 80, 90, 180); //Right arc

            /**  Black border  **/
            g.DrawLine(blackPen, 100, 0, (float)_curSpecs.RinkWidth - 100, 0); //Top black line
            g.DrawLine(blackPen, 1, 100, 1, (float)_curSpecs.RinkHeight - 100); //Left black line
            g.DrawLine(blackPen, (float)_curSpecs.RinkWidth - 1, 100, (float)_curSpecs.RinkWidth - 1, (float)_curSpecs.RinkHeight - 100); //Right black line
            g.DrawLine(blackPen, 100, (float)_curSpecs.RinkHeight - 1, (float)_curSpecs.RinkWidth - 100, (float)_curSpecs.RinkHeight - 1); //Bottom black line
            g.DrawArc(blackPen, 0, 0, 200, 200, 180, 90); //Top left curve
            g.DrawArc(blackPen, 0, (float)_curSpecs.RinkHeight - 201, 200, 200, 180, -90); //Bottom left curve
            g.DrawArc(blackPen, (float)_curSpecs.RinkWidth - 201, 0, 200, 200, 270, 90); //Top right curve
            g.DrawArc(blackPen, (float)_curSpecs.RinkWidth - 201, (float)_curSpecs.RinkHeight - 201, 200, 200, 90, -90); //Bottom right curve

            /**  Dots  **/
            g.FillEllipse(blueBrush, rinkCenterX - (float)_curSpecs.DotRadius, rinkCenterY - (float)_curSpecs.DotRadius, (float)_curSpecs.DotRadius * 2, (float)_curSpecs.DotRadius * 2); //Center dot
            g.FillEllipse(redBrush, (float)_curSpecs.EdgeToCircle - (float)_curSpecs.DotRadius, rinkCenterY - (float)_curSpecs.CenterNetToCircle - (float)_curSpecs.DotRadius, (float)_curSpecs.DotRadius * 2, (float)_curSpecs.DotRadius * 2); //Top left
            g.FillEllipse(redBrush, (float)_curSpecs.RinkWidth - (float)_curSpecs.EdgeToCircle - (float)_curSpecs.DotRadius, rinkCenterY - (float)_curSpecs.CenterNetToCircle - (float)_curSpecs.DotRadius, (float)_curSpecs.DotRadius * 2, (float)_curSpecs.DotRadius * 2); //Top right circle
            g.FillEllipse(redBrush, (float)_curSpecs.EdgeToCircle - (float)_curSpecs.DotRadius, rinkCenterY + (float)_curSpecs.CenterNetToCircle - (float)_curSpecs.DotRadius, (float)_curSpecs.DotRadius * 2, (float)_curSpecs.DotRadius * 2); //Bottom left circle
            g.FillEllipse(redBrush, (float)_curSpecs.RinkWidth - (float)_curSpecs.EdgeToCircle - (float)_curSpecs.DotRadius, rinkCenterY + (float)_curSpecs.CenterNetToCircle - (float)_curSpecs.DotRadius, (float)_curSpecs.DotRadius * 2, (float)_curSpecs.DotRadius * 2); //Bottom right circle
            g.FillEllipse(redBrush, rinkCenterX - (float)_curSpecs.AuxDotsFromCenter - (float)_curSpecs.DotRadius, rinkCenterY - (float)_curSpecs.CenterNetToCircle - (float)_curSpecs.DotRadius, (float)_curSpecs.DotRadius * 2, (float)_curSpecs.DotRadius * 2);
            g.FillEllipse(redBrush, rinkCenterX + (float)_curSpecs.AuxDotsFromCenter - (float)_curSpecs.DotRadius, rinkCenterY - (float)_curSpecs.CenterNetToCircle - (float)_curSpecs.DotRadius, (float)_curSpecs.DotRadius * 2, (float)_curSpecs.DotRadius * 2);
            g.FillEllipse(redBrush, rinkCenterX - (float)_curSpecs.AuxDotsFromCenter - (float)_curSpecs.DotRadius, rinkCenterY + (float)_curSpecs.CenterNetToCircle - (float)_curSpecs.DotRadius, (float)_curSpecs.DotRadius * 2, (float)_curSpecs.DotRadius * 2);
            g.FillEllipse(redBrush, rinkCenterX + (float)_curSpecs.AuxDotsFromCenter - (float)_curSpecs.DotRadius, rinkCenterY + (float)_curSpecs.CenterNetToCircle - (float)_curSpecs.DotRadius, (float)_curSpecs.DotRadius * 2, (float)_curSpecs.DotRadius * 2);

            /**  Circles  **/
            g.DrawEllipse(redPen, (float)_curSpecs.EdgeToCircle - (float)_curSpecs.CircleRadius, rinkCenterY - (float)_curSpecs.CenterNetToCircle - (float)_curSpecs.CircleRadius, (float)_curSpecs.CircleRadius * 2, (float)_curSpecs.CircleRadius * 2); //Top left
            g.DrawEllipse(redPen, (float)_curSpecs.RinkWidth - (float)_curSpecs.EdgeToCircle - (float)_curSpecs.CircleRadius, rinkCenterY - (float)_curSpecs.CenterNetToCircle - (float)_curSpecs.CircleRadius, (float)_curSpecs.CircleRadius * 2, (float)_curSpecs.CircleRadius * 2); //Top right
            g.DrawEllipse(redPen, (float)_curSpecs.EdgeToCircle - (float)_curSpecs.CircleRadius, rinkCenterY + (float)_curSpecs.CenterNetToCircle - (float)_curSpecs.CircleRadius, (float)_curSpecs.CircleRadius * 2, (float)_curSpecs.CircleRadius * 2); //Bottom left
            g.DrawEllipse(redPen, (float)_curSpecs.RinkWidth - (float)_curSpecs.EdgeToCircle - (float)_curSpecs.CircleRadius, rinkCenterY + (float)_curSpecs.CenterNetToCircle - (float)_curSpecs.CircleRadius, (float)_curSpecs.CircleRadius * 2, (float)_curSpecs.CircleRadius * 2); //Bottom right
            g.DrawEllipse(bluePen, rinkCenterX - (float)_curSpecs.CircleRadius, rinkCenterY - (float)_curSpecs.CircleRadius, (float)_curSpecs.CircleRadius * 2, (float)_curSpecs.CircleRadius * 2);
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
                    var x = colorDialog1.Color.ToArgb();
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
                for (var i = 0; i < sc.Count; i++)
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
            _currentPlay.Objects.Clear();
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
            redraw();
            Play result = null;
            if (path.ToUpper().EndsWith(".PLY"))
                result = Plays.LoadPLYFile(path);
            else if (path.ToUpper().EndsWith(".PLYX"))
            {
                result = Plays.LoadPLYXFile(path);
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
                txtPlayDesc.Text = result.Description;
                _currentPlay = result;
                _curSpecs = RinkSpecs.GetRink(result.RinkType, _requiredScale);
                addRecentFile(path);
                redraw();
                updateTitlebar(Path.GetFileName(path));
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!checkSaved("opening a new play")) return;
            var dlg = new OpenFileDialog
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
            var d = MessageBox.Show("The current play is not saved. Do you want to save before " + operation + "?", "Unsaved Play", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button3);
            if (d == DialogResult.Yes)
                saveToolStripMenuItem_Click(saveToolStripMenuItem, null);
            else if (d == DialogResult.Cancel)
                return false;
            return true;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_currentFile == "") return;
            if (!Plays.SavePlyxFile(_currentFile, _currentPlay, txtPlayName.Text, txtPlayDesc.Text)) return;
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
            using (var ld = new frmManage())
            {
                if (ld.ShowDialog() == DialogResult.Yes && ld.openPlay != "")
                    openFile(ld.openPlay, false);
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var ld = new frmSaveAs(txtPlayName.Text))
            {
                if (ld.ShowDialog() == DialogResult.Yes && Plays.SavePlyxFile(ld.fileName, _currentPlay, ld.playName, txtPlayDesc.Text))
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
                var size = g.MeasureString(text, font);
                if (size.Height <= proposedSize.Height && size.Width <= proposedSize.Width)
                    return font;

                var oldFont = font;
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
            var pp = new PrintPreviewDialog { Document = pd };
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
                foreach (var play in plays)
                {
                    if (play == "")
                    {
                        posy += 250.0f;
                        continue;
                    }
                    var result = Plays.LoadPLYXFile(play);
                    var rink = new Bitmap(1000, 500);
                    using (var g = Graphics.FromImage(rink))
                    {
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        drawRink(g);
                        g.SmoothingMode = SmoothingMode.None;
                        foreach (var obj in result.Objects)
                        {
                            obj.Draw(g, false);
                        }
                    }
                    pe.Graphics.DrawLine(new Pen(Color.Black, 1.0f), 0.0f, posy, 1100.0f, posy);
                    posy += 5.0f;
                    var newWidth = (int)(240.0 / rink.Height * rink.Width);
                    pe.Graphics.DrawImage(rink, new Rectangle(50, (int)posy, newWidth, 240));
                    var nameFont = ShrinkFont(pe.Graphics, result.Name, new Font("Helvetica", 11.0f, FontStyle.Bold), new Size(750 - newWidth - 10, 100));
                    var nameSize = pe.Graphics.MeasureString(result.Name, nameFont);
                    pe.Graphics.DrawString(result.Name, nameFont, new SolidBrush(Color.Black), 50.0f + newWidth + 10.0f, posy);
                    var descFont = ShrinkFont(pe.Graphics, result.Description, new Font("Helvetica", 8.0f), new Size(750 - newWidth - 10, 240 - (int)nameSize.Height - 5));
                    pe.Graphics.DrawString(result.Description, descFont, new SolidBrush(Color.Black), 50.0f + newWidth + 10.0f, posy + nameSize.Height + 5.0f);
                    posy += 245.0f;
                    pe.Graphics.DrawLine(new Pen(Color.Black, 1.0f), 0.0f, posy, 1100.0f, posy);
                    rink.Dispose();
                }
                posy += 25.0f;
                pe.Graphics.DrawString("CoachDraw © 2018", new Font("Helvetica", 5.0f), new SolidBrush(Color.Black), 50, posy);
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

        private void RinkTypeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _currentPlay.RinkType = (RinkType) Enum.Parse(typeof(RinkType), ((ToolStripComboBox) sender).SelectedItem.ToString());
            _curSpecs = RinkSpecs.GetRink(_currentPlay.RinkType, _requiredScale);
            redraw();
        }
    }

    public enum RinkType
    {
        IIHF,
        NHL
    }


    //public abstract double BlueLineFromCenter = 30
    //public double AuxDotsFromCenter = 22;
    //public double RedToBlue = 60;
    //public double EdgeToRed = 12;
    //public double CenterNetToCircle = 22;
    //public double RedToCircle = 20;
    //public double EdgeToCircle = 32;
    //public double NetWidth = 4;
    //public double NetLength = 6;
    //public double NetArcRadius = 8;
    //public double CircleRadius = 15;
    //public double DotRadius = 2.5;
    //public double HashMarkSize = 4;

    public class NHLRink : RinkSpecs
    {
        public override double RinkWidth { get; set; } = 200;
        public override double RinkHeight { get; set; } = 85;
        public override double BlueLineFromCenter { get; set; } = 25;
        public override double AuxDotsFromCenter { get; set; } = 20;
        public override double RedToBlue { get; set; } = 64;
        public override double EdgeToRed { get; set; } = 12;
        public override double CenterNetToCircle { get; set; } = 22;
        //public override double RedToCircle { get; set; } = 20;
        public override double EdgeToCircle { get; set; } = 32;
        public override double NetWidth { get; set; } = 4;
        public override double NetLength { get; set; } = 6;
        public override double NetArcRadius { get; set; } = 8;
        public override double CircleRadius { get; set; } = 15;
        public override double DotRadius { get; set; } = 1;
        public override double HashMarkSize { get; set; } = 4;
        public override bool GoalieTrapezoid { get; set; } = true;

        public NHLRink(double newScale) : base(newScale)
        {
        }
    }

    public class IIHFRink : RinkSpecs
    {
        public override double RinkWidth { get; set; } = 200;
        public override double RinkHeight { get; set; } = 100;
        public override double BlueLineFromCenter { get; set; } = 30;
        public override double AuxDotsFromCenter { get; set; } = 22;
        public override double RedToBlue { get; set; } = 60;
        public override double EdgeToRed { get; set; } = 12;
        public override double CenterNetToCircle { get; set; } = 22;
        //public override double RedToCircle { get; set; } = 20;
        public override double EdgeToCircle { get; set; } = 32;
        public override double NetWidth { get; set; } = 4;
        public override double NetLength { get; set; } = 6;
        public override double NetArcRadius { get; set; } = 8;
        public override double CircleRadius { get; set; } = 15;
        public override double DotRadius { get; set; } = 1;
        public override double HashMarkSize { get; set; } = 4;
        public override bool GoalieTrapezoid { get; set; } = false;

        public IIHFRink(double newScale) : base(newScale)
        {
        }
    }

    // http://www.nhl.com/nhl/en/v3/ext/rules/2017-2018-NHL-rulebook.pdf
    // http://www.iihf.com/fileadmin/user_upload/PDF/Sport/IIHF_Official_Rule_Book_2018.pdf
    public abstract class RinkSpecs
    {
        //Distances in feet
        public abstract double RinkWidth { get; set; }
        public abstract double RinkHeight { get; set; }
        public abstract double BlueLineFromCenter { get; set; }
        public abstract double AuxDotsFromCenter { get; set; }
        public abstract double RedToBlue { get; set; }
        public abstract double EdgeToRed { get; set; }
        public abstract double CenterNetToCircle { get; set; }
        //public abstract double RedToCircle { get; set; }
        public abstract double EdgeToCircle { get; set; }
        public abstract double NetWidth { get; set; }
        public abstract double NetLength { get; set; }
        public abstract double NetArcRadius { get; set; }
        public abstract double CircleRadius { get; set; }
        public abstract double DotRadius { get; set; }
        public abstract double HashMarkSize { get; set; }
        public abstract bool GoalieTrapezoid { get; set; }
        private double _curScale = 1;

        protected RinkSpecs(double newScale)
        {
            SetScale(newScale);
        }

        public void SetScale(double newScale)
        {
            RinkWidth /= _curScale; RinkWidth *= newScale;
            RinkHeight /= _curScale; RinkHeight *= newScale;
            BlueLineFromCenter /= _curScale; BlueLineFromCenter *= newScale;
            AuxDotsFromCenter /= _curScale; AuxDotsFromCenter *= newScale;
            RedToBlue /= _curScale; RedToBlue *= newScale;
            EdgeToRed /= _curScale; EdgeToRed *= newScale;
            CenterNetToCircle /= _curScale; CenterNetToCircle *= newScale;
            //RedToCircle /= _curScale; RedToCircle *= newScale;
            EdgeToCircle /= _curScale; EdgeToCircle *= newScale;
            NetWidth /= _curScale; NetWidth *= newScale;
            NetLength /= _curScale; NetLength *= newScale;
            NetArcRadius /= _curScale; NetArcRadius *= newScale;
            CircleRadius /= _curScale; CircleRadius *= newScale;
            DotRadius /= _curScale; DotRadius *= newScale;
            _curScale = newScale;
        }

        public static RinkSpecs GetRink(RinkType type, double scale)
        {
            switch (type)
            {
                case RinkType.IIHF:
                    return new IIHFRink(scale);
                case RinkType.NHL:
                    return  new NHLRink(scale);
            }
            throw new InvalidEnumArgumentException();
        }
    }
}
