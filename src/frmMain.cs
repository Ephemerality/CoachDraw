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
using CoachDraw.Drawables;
using CoachDraw.Drawables.Items;
using CoachDraw.Drawables.Lines;
using CoachDraw.Rink;

namespace CoachDraw
{
    public partial class FrmMain : Form
    {
        /** Path compacting from snippet on MSDN **/
        [DllImport("shlwapi.dll")]
        private static extern bool PathCompactPathEx([Out] StringBuilder pszOut, string szPath, int cchMax, int dwFlags);

        private static string CompactString(string path, int length)
        {
            var sb = new StringBuilder(length + 1);
            PathCompactPathEx(sb, path, length + 1, 0);
            return sb.ToString();
        }
        /******************************************/
        private const double RequiredScale = 5;
        private Point _startPoint;
        private Point _endPoint;
        private bool _mouseDown;
        private Bitmap _snapshot;
        private Bitmap _tempDraw;
        private readonly List<Point> _tempcoords;
        private Play _currentPlay;
        private RinkSpecs _curSpecs = new IihfRink(RequiredScale); //Scale
        private string _selectedTool = "Line";
        private string _currentFile = "";
        private bool _saved = true;
        private Drawable _selected;
        private string _lastSelectedMultiPrint = "";
        private List<string> _lastSetMultiPrint = new() { "", "", "", "" };

        public FrmMain()
        {
            InitializeComponent();
            _snapshot = new Bitmap(panel1.ClientRectangle.Width, ClientRectangle.Height);
            _tempDraw = (Bitmap)_snapshot.Clone();
            _tempcoords = new List<Point>();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            _currentPlay = new Play();
            UpdateRecentFiles();
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
            Redraw();
            UpdateTitlebar();
        }

        private void UpdateTitlebar(string text = "")
        {
            if (_currentFile == "")
                text = "Untitled";
            if (text == "")
                text = Path.GetFileName(_currentFile);
            Text = $"{(_saved ? "" : "*")}{text} - Coach Draw v{Application.ProductVersion}";
            saveToolStripMenuItem.Enabled = _currentFile != "";
        }

        private void SelectObj(object sender, EventArgs e)
        {
            var mn = (ToolStripMenuItem)sender;
            if ((int)mn.Tag == -1) return;
            _selected = _currentPlay.Objects[(int)mn.Tag];
            Redraw();
        }

        private void DeselectObj(object sender, ToolStripDropDownClosedEventArgs e)
        {
            _selected = null;
            Redraw();
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
                    if (_currentPlay.Objects[i].HitBox != null && _currentPlay.Objects[i].HitBox.IsVisible(e.X, e.Y) ||
                        _currentPlay.Objects[i].Line != null && _currentPlay.Objects[i].Line.HitBox != null && _currentPlay.Objects[i].Line.HitBox.IsVisible(e.X, e.Y))
                    {
                        hits.Add(i);
                    }
                }
                var mn = new ContextMenuStrip();
                mn.Closed += DeselectObj;
                if (hits.Count == 1)
                {
                    mn.Items.AddRange(BuildMenu((int)_currentPlay.Objects[hits[0]].Item.Type,
                        _currentPlay.Objects[hits[0]].Line == null ? -1 : (int)_currentPlay.Objects[hits[0]].Line.LineType,
                        _currentPlay.Objects[hits[0]].Line == null ? -1 : (int)_currentPlay.Objects[hits[0]].Line.EndType));
                    mn.Tag = hits[0];
                    _selected = _currentPlay.Objects[hits[0]];
                    Redraw();
                }
                else if (hits.Count > 1)
                {
                    var hitsMenu = new List<ToolStripMenuItem>();
                    foreach (var hit in hits)
                    {
                        var newMn = new ToolStripMenuItem(hit.ToString());
                        newMn.DropDownItems.AddRange(BuildMenu((int)_currentPlay.Objects[hit].Item.Type,
                            _currentPlay.Objects[hit].Line == null ? -1 : (int)_currentPlay.Objects[hit].Line.LineType,
                            _currentPlay.Objects[hit].Line == null ? -1 : (int)_currentPlay.Objects[hit].Line.EndType));
                        newMn.Tag = hit;
                        newMn.MouseHover += SelectObj;
                        hitsMenu.Add(newMn);
                    }
                    mn.Items.AddRange(hitsMenu.ToArray<ToolStripItem>());
                    mn.Tag = -1;
                }
                if (mn.Items.Count > 0)
                    mn.Show(panel1, new Point(e.X, e.Y));
            }
            else if (e.Button == MouseButtons.Left)
            {
                if (!_mouseDown) return;
                _mouseDown = false;
                if (_currentPlay.Objects.Any(o => o.Location.Equals(_startPoint)))
                    return;
                _endPoint.X = Math.Min(e.X, panel1.Width);
                _endPoint.Y = Math.Min(e.Y, panel1.Height);

                var itemType = (Item.TypeEnum)Enum.Parse(typeof(Item.TypeEnum), ItemTypeBox.Text.Replace(" ", "").Replace(".", ""));
                var playerNumber = PlayerNumBox.Text == @"None"
                    ? (int?) null
                    : int.Parse(PlayerNumBox.Text);
                var drawableItem = ItemBuilder.Build(itemType, playerNumber);
                Line line = null;
                if (!_startPoint.Equals(_endPoint) || _tempcoords.Count > 0)
                {
                    line = new Line
                    {
                        LineType = (LineType)Enum.Parse(typeof(LineType), LineTypeBox.Text.Replace(" ", "")),
                        EndType = (EndType)Enum.Parse(typeof(EndType), EndTypeBox.Text.Replace(" ", "")),
                        Color = colorDialog1.Color
                    };
                    foreach (var item in widthBox.DropDownItems.OfType<ToolStripMenuItem>().Where(item => item.Checked))
                        line.LineWidth = byte.Parse(item.Text.Remove(1));

                    var lineLength = 0;
                    if (_selectedTool == "Line")
                    {
                        lineLength = Smoothing.GetLineLength(_startPoint, _endPoint);
                        line.Points.Add(_startPoint);
                        line.Points.Add(_endPoint);
                    }
                    else if (_selectedTool == "Pencil")
                    {
                        line.Points = _tempcoords.ToList();
                        lineLength = line.GetAggregateLength(0, line.Points.Count - 1);
                    }
                    line.CleanDuplicates();
                    if (lineLength < 20 || line.Points.Count < 2)
                        line = null;
                }

                var drawable = new Drawable(_startPoint, drawableItem)
                {
                    Color = colorDialog1.Color,
                    Line = line
                };

                _currentPlay.Objects.Add(drawable);
                _saved = false;
                UpdateTitlebar();
            }
            Redraw();
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

        private void ClickItemList(object sender, EventArgs e)
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
                    var itemType = (Item.TypeEnum)Enum.Parse(typeof(Item.TypeEnum), mn.Text);
                    _currentPlay.Objects[i].Item = ItemBuilder.Build(itemType, _currentPlay.Objects[i].Item.Number);
                    break;
                case 1:
                    if (mn.Text == "Delete Line")
                    {
                        if (MessageBox.Show("Deleting this line cannot be undone. Are you sure you want to continue?", "Delete Line", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                            _currentPlay.Objects[i].Line = null;
                    }
                    else
                        _currentPlay.Objects[i].Line.LineType = (LineType)Enum.Parse(typeof(LineType), mn.Text);
                    break;
                case 2:
                    _currentPlay.Objects[i].Line.EndType = (EndType)Enum.Parse(typeof(EndType), mn.Text);
                    break;
            }
            _saved = false;
            _selected = null;
            Redraw();
            UpdateTitlebar();
        }

        private void DeleteItem(object sender, EventArgs e)
        {
            var mn = (ToolStripMenuItem)sender;
            var i = mn.Owner.GetType() == typeof(ContextMenuStrip) ? (int)mn.Owner.Tag : (int)mn.OwnerItem.Tag;
            _currentPlay.Objects.RemoveAt(i);
            _saved = false;
            Redraw();
            UpdateTitlebar();
        }

        // TODO: Review
        private ToolStripItem[] BuildMenu(int it, int lt, int et)
        {
            var itemList = new List<ToolStripItem>();
            var lineList = new List<ToolStripItem>();
            var endList = new List<ToolStripItem>();
            foreach (var itemtype in Enum.GetNames(typeof(Item.TypeEnum)))
            {
                var m = new ToolStripMenuItem(itemtype, null, ClickItemList);
                if (it != -1 && Enum.GetName(typeof(Item.TypeEnum), it).Equals(itemtype)) { m.Checked = true; }
                itemList.Add(m);
            }
            lineList.Add(new ToolStripMenuItem("Delete Line", null, ClickItemList));
            lineList.Add(new ToolStripSeparator());
            foreach (var linetype in Enum.GetNames(typeof(LineType)))
            {
                var m = new ToolStripMenuItem(linetype, null, ClickItemList);
                if (lt != -1 && !m.Checked && Enum.GetName(typeof(LineType), lt).Equals(linetype)) { m.Checked = true; }
                lineList.Add(m);
            }
            foreach (var endtype in Enum.GetNames(typeof(EndType)))
            {
                var m = new ToolStripMenuItem(endtype, null, ClickItemList);
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
            mn[4] = new ToolStripMenuItem("Delete", null, DeleteItem);
            return mn;
        }

        private void Redraw()
        {
            _snapshot.Dispose();
            _snapshot = new Bitmap(panel1.ClientRectangle.Width, panel1.ClientRectangle.Height);
            using (var g = Graphics.FromImage(_snapshot))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                DrawRink(g);
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
        private void DrawRink(Graphics g)
        {
            using var blackPen = new Pen(Color.Black, 2);
            using var redPen = new Pen(Color.Red, 2);
            using var bluePen = new Pen(Color.Blue, 2);
            using var redBrush = new SolidBrush(Color.Red);
            using var blueBrush = new SolidBrush(Color.Blue);
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
        private void AddRecentFile(string path)
        {
            var sc = Properties.Settings.Default.recentFiles ?? new StringCollection();
            if (sc.Contains(path)) return;
            sc.Add(path);
            if (sc.Count > 5) sc.RemoveAt(0);
            UpdateRecentFiles();
        }

        private void UpdateRecentFiles()
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
                    var tempItem = new ToolStripMenuItem(CompactString(path, 50), null, ClickRecentFile) { Tag = path };
                    recentFilesToolStripMenuItem.DropDownItems.Add(tempItem);
                }
                Properties.Settings.Default.recentFiles = sc;
                Properties.Settings.Default.Save();
                recentFilesToolStripMenuItem.Enabled = sc.Count > 0;
            }
            else
                recentFilesToolStripMenuItem.Enabled = false;
        }

        private void ClickRecentFile(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            if (item.Tag != null)
                OpenFile((string)item.Tag, false);
        }
        #endregion

        #region Toolbar/saving/opening stuff
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CheckSaved("starting a new play")) return;
            txtPlayName.Text = "";
            txtPlayDesc.Text = "";
            _currentFile = "";
            _saved = true;
            _currentPlay.Objects.Clear();
            Redraw();
            UpdateTitlebar("Untitled");
        }

        private void OpenFile(string path, bool skipSaveCheck)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show("Sorry, that file no longer exists!", "File Not Found");
                return;
            }
            if (!skipSaveCheck && !CheckSaved("opening a new play")) return;
            Redraw();
            Play result = null;
            if (path.ToUpper().EndsWith(".PLY"))
                result = Plays.LoadPlyFile(path);
            else if (path.ToUpper().EndsWith(".PLYX"))
            {
                result = Plays.LoadPlyxFile(path);
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
                _curSpecs = RinkSpecs.GetRink(result.RinkType, RequiredScale);
                AddRecentFile(path);
                Redraw();
                UpdateTitlebar(Path.GetFileName(path));
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CheckSaved("opening a new play")) return;
            var dlg = new OpenFileDialog
            {
                Filter = "All supported files (*.ply, *.plyx)|*.ply;*.plyx|New play files (*.plyx)|*.plyx|HockeyVision play files (*.ply)|*.ply|All files (*.*)|*.*",
                FilterIndex = 3
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                OpenFile(dlg.FileName, true);
        }

        private bool CheckSaved(string operation)
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
            UpdateTitlebar();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CheckSaved("exiting"))
                Application.Exit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!CheckSaved("exiting"))
                e.Cancel = true;
        }
        #endregion

        private void openManagedPlayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var ld = new FrmManage();
            if (ld.ShowDialog() == DialogResult.Yes && ld.OpenPlay != "")
                OpenFile(ld.OpenPlay, false);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var ld = new FrmSaveAs(txtPlayName.Text);
            if (ld.ShowDialog() == DialogResult.Yes && Plays.SavePlyxFile(ld.FileName, _currentPlay, ld.PlayName, txtPlayDesc.Text))
            {
                txtPlayName.Text = ld.PlayName;
                _saved = true;
                _currentFile = ld.FileName;
                AddRecentFile(ld.FileName);
                UpdateTitlebar();
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
                pe.Graphics.DrawString("CoachDraw © 2021", new Font("Helvetica", 5.0f), new SolidBrush(Color.Black), 50, 825.0f);
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
            using var mp = new FrmMultiPrint(_currentFile, _lastSelectedMultiPrint, _lastSetMultiPrint);
            if (mp.ShowDialog() == DialogResult.Yes)
                PrintMultiplePlays(mp.Plays);
            _lastSelectedMultiPrint = mp.LastSelected;
            _lastSetMultiPrint = mp.Plays ?? new List<string> { "", "", "", "" };
        }

        private void PrintMultiplePlays(List<string> plays)
        {
            var pd = new PrintDocument();
            pd.PrintPage += (_, pe) =>
            {
                float posy = 50;
                foreach (var play in plays)
                {
                    if (play == "")
                    {
                        posy += 250.0f;
                        continue;
                    }
                    var result = Plays.LoadPlyxFile(play);
                    var rink = new Bitmap(1000, 500);
                    using (var g = Graphics.FromImage(rink))
                    {
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        DrawRink(g);
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
            _curSpecs = RinkSpecs.GetRink(_currentPlay.RinkType, RequiredScale);
            Redraw();
        }
    }
}
