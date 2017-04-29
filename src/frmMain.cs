using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
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

        int rinkWidth = 1000;
        int rinkHeight = 500;
        Point startPoint;
        Point endPoint;
        bool mouseDown;
        Bitmap snapshot;
        Bitmap tempDraw;
        List<Point> tempcoords;
        List<drawObj> objs;
        RinkSpecs curSpecs = new RinkSpecs(5); //Scale
        string selectedTool = "Line";
        string currentFile = "";
        bool saved = true;
        uint plyxVersion = 1;
        drawObj selected = null;
        string lastSelectedMultiPrint = "";
        List<string> lastSetMultiPrint = new List<string>() { "", "", "", "" };

        public frmMain()
        {
            InitializeComponent();
            snapshot = new Bitmap(panel1.ClientRectangle.Width, this.ClientRectangle.Height);
            tempDraw = (Bitmap)snapshot.Clone();
            tempcoords = new List<Point>();
            objs = new List<drawObj>();
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
            if (currentFile == "") text = "Untitled";
            if (text == "") text = Path.GetFileName(currentFile);
            this.Text = (saved ? "" : "*") + text + " - Coach Draw v" + Application.ProductVersion;
            saveToolStripMenuItem.Enabled = currentFile != "";
        }

        void selectObj(object sender, EventArgs e)
        {
            ToolStripMenuItem mn = (ToolStripMenuItem)sender;
            if ((int)mn.Tag == -1) return;
            selected = objs[(int)mn.Tag];
            redraw();
        }

        void deselectObj(object sender, ToolStripDropDownClosedEventArgs e)
        {
            selected = null;
            redraw();
        }

        #region Panel Events
        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            mouseDown = true;
            startPoint.X = e.X;
            startPoint.Y = e.Y;
            if (selectedTool == "Pencil")
            {
                tempcoords.Clear();
                tempcoords.Add(new Point(startPoint.X, startPoint.Y));
            }
            tempDraw = (Bitmap)snapshot.Clone();
        }

        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            selected = null;
            if (e.Button == MouseButtons.Right)
            {

                List<int> hits = new List<int>();
                for (int i = 0; i < objs.Count; i++)
                {
                    if ((objs[i].hitBox != null && objs[i].hitBox.IsVisible(e.X, e.Y)) ||
                        (objs[i].objLine != null && objs[i].objLine.hitBox != null && objs[i].objLine.hitBox.IsVisible(e.X, e.Y)))
                    {
                        hits.Add(i);
                    }
                }
                ContextMenuStrip mn = new ContextMenuStrip();
                mn.Closed += new ToolStripDropDownClosedEventHandler(deselectObj);
                if (hits.Count == 1)
                {
                    mn.Items.AddRange(buildMenu((int)objs[hits[0]].objType, (objs[hits[0]].objLine == null ? -1 : (int)objs[hits[0]].objLine.lineType),
                            (objs[hits[0]].objLine == null ? -1 : (int)objs[hits[0]].objLine.endType)));
                    mn.Tag = hits[0];
                    selected = objs[hits[0]];
                    redraw();
                }
                else if (hits.Count > 1)
                {
                    List<ToolStripMenuItem> hitsMenu = new List<ToolStripMenuItem>();
                    for (int i = 0; i < hits.Count; i++)
                    {
                        ToolStripMenuItem newMn = new ToolStripMenuItem(hits[i].ToString());
                        newMn.DropDownItems.AddRange(buildMenu((int)objs[hits[i]].objType, (objs[hits[i]].objLine == null ? -1 : (int)objs[hits[i]].objLine.lineType),
                            (objs[hits[i]].objLine == null ? -1 : (int)objs[hits[i]].objLine.endType)));
                        newMn.Tag = hits[i];
                        newMn.MouseHover += new EventHandler(selectObj);
                        hitsMenu.Add(newMn);
                    }
                    mn.Items.AddRange(hitsMenu.ToArray());
                    mn.Tag = -1;
                }
                if (mn.Items.Count > 0) mn.Show(panel1, new Point(e.X, e.Y));
            }
            else if (e.Button == MouseButtons.Left)
            {
                if (!mouseDown) return;
                mouseDown = false;
                foreach (drawObj o in objs)
                {
                    if (o.objLoc.Equals(startPoint))
                        return;
                }
                endPoint.X = Math.Min(e.X, panel1.Width);
                endPoint.Y = Math.Min(e.Y, panel1.Height);
                drawObj newObj = new drawObj();
                newObj.objType = (ItemType)Enum.Parse(typeof(ItemType), ItemTypeBox.Text.Replace(" ", "").Replace(".", ""));
                newObj.objLoc = startPoint;
                newObj.color = colorDialog1.Color;
                newObj.objLabel = (PlayerNumBox.Text == "None" ? -1 : int.Parse(PlayerNumBox.Text));
                if (!startPoint.Equals(endPoint) || tempcoords.Count > 0)
                {
                    newObj.objLine = new Line();
                    newObj.objLine.lineType = (LineType)Enum.Parse(typeof(LineType), LineTypeBox.Text.Replace(" ", ""));
                    newObj.objLine.endType = (EndType)Enum.Parse(typeof(EndType), EndTypeBox.Text.Replace(" ", ""));
                    newObj.objLine.color = colorDialog1.Color;
                    foreach (ToolStripMenuItem t in widthBox.DropDownItems)
                        if (t.Checked) newObj.objLine.lineWidth = byte.Parse(t.Text.Remove(1));

                    int lineLength = 0;
                    if (selectedTool == "Line")
                    {
                        lineLength = Smoothing.getLineLength(startPoint, endPoint);
                        newObj.objLine.points.Add(startPoint);
                        newObj.objLine.points.Add(endPoint);
                    }
                    else if (selectedTool == "Pencil")
                    {
                        newObj.objLine.points = tempcoords.ToList<Point>();
                        lineLength = newObj.objLine.getAggregateLength(0, newObj.objLine.points.Count - 1);
                    }
                    newObj.objLine.cleanDuplicates();
                    if (lineLength < 20 || newObj.objLine.points.Count < 2) newObj.objLine = null;
                }
                objs.Add(newObj);
                saved = false;
                updateTitlebar();
            }
            redraw();
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                endPoint.X = Math.Min(e.X, panel1.Width);
                endPoint.Y = Math.Min(e.Y, panel1.Height);
                panel1.Invalidate();
                panel1.Update();
            }
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            if (tempDraw != null && mouseDown)
            {
                Graphics g = Graphics.FromImage(tempDraw);
                Pen myPen = new Pen(Color.Black, 1);
                if (selectedTool == "Line")
                    g.DrawLine(myPen, startPoint, endPoint);
                else if (selectedTool == "Pencil")
                {
                    tempcoords.Add(endPoint);
                    g.DrawLines(myPen, tempcoords.ToArray());
                }
                myPen.Dispose();
                e.Graphics.DrawImageUnscaled(tempDraw, 0, 0);
                g.Dispose();
                tempDraw = (Bitmap)snapshot.Clone();
            }
            else if (snapshot != null)
                e.Graphics.DrawImageUnscaled(snapshot, 0, 0);
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
                    if ((int)next.Owner.Tag == -1) i = (int)next.Tag;
                    else i = (int)next.Owner.Tag;
                    break;
                }
                next = next.OwnerItem;
            }
            switch ((int)mn.OwnerItem.Tag)
            {
                case 0:
                    objs[i].objType = (ItemType)Enum.Parse(typeof(ItemType), mn.Text);
                    break;
                case 1:
                    if (mn.Text == "Delete Line")
                    {
                        if (MessageBox.Show("Deleting this line cannot be undone. Are you sure you want to continue?", "Delete Line", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                            objs[i].objLine = null;
                    }
                    else
                        objs[i].objLine.lineType = (LineType)Enum.Parse(typeof(LineType), mn.Text);
                    break;
                case 2:
                    objs[i].objLine.endType = (EndType)Enum.Parse(typeof(EndType), mn.Text);
                    break;
                default:
                    break;
            }
            saved = false;
            selected = null;
            redraw();
            updateTitlebar();
        }

        private void deleteItem(object sender, EventArgs e)
        {
            ToolStripMenuItem mn = (ToolStripMenuItem)sender;
            int i;
            if (mn.Owner.GetType() == typeof(ContextMenuStrip))
               i = (int)mn.Owner.Tag;
            else
               i = (int)mn.OwnerItem.Tag;
            objs.RemoveAt(i);
            saved = false;
            redraw();
            updateTitlebar();
        }

        private ToolStripItem[] buildMenu(int it, int lt, int et)
        {
            List<ToolStripItem> itemList = new List<ToolStripItem>();
            List<ToolStripItem> lineList = new List<ToolStripItem>();
            List<ToolStripItem> endList = new List<ToolStripItem>();
            foreach (string itemtype in Enum.GetNames(typeof(ItemType)))
            {
                ToolStripMenuItem m = new ToolStripMenuItem(itemtype, null, new EventHandler(clickItemList));
                if (it != -1 && Enum.GetName(typeof(ItemType), it).Equals(itemtype)) { m.Checked = true; }
                itemList.Add(m);
            }
            lineList.Add(new ToolStripMenuItem("Delete Line", null, new EventHandler(clickItemList)));
            lineList.Add(new ToolStripSeparator());
            foreach (string linetype in Enum.GetNames(typeof(LineType)))
            {
                ToolStripMenuItem m = new ToolStripMenuItem(linetype, null, new EventHandler(clickItemList));
                if (lt != -1 && !m.Checked && Enum.GetName(typeof(LineType), lt).Equals(linetype)) { m.Checked = true; }
                lineList.Add(m);
            }
            foreach (string endtype in Enum.GetNames(typeof(EndType)))
            {
                ToolStripMenuItem m = new ToolStripMenuItem(endtype, null, new EventHandler(clickItemList));
                if (et != -1 && !m.Checked && Enum.GetName(typeof(EndType), et).Equals(endtype)) { m.Checked = true; }
                endList.Add(m);
            }
            ToolStripItem[] mn = new ToolStripItem[5];
            mn[0] = new ToolStripMenuItem("Item Type", null, itemList.ToArray());
            mn[0].Tag = 0;
            mn[1] = new ToolStripMenuItem("Line Type", null, lineList.ToArray());
            mn[1].Tag = 1;
            if (lt == -1) mn[1].Enabled = false;
            mn[2] = new ToolStripMenuItem("End Type", null, endList.ToArray());
            mn[2].Tag = 2;
            if (et == -1) mn[2].Enabled = false;
            mn[3] = new ToolStripSeparator();
            mn[4] = new ToolStripMenuItem("Delete", null, new EventHandler(deleteItem));
            return mn;
        }

        private void redraw()
        {
            snapshot.Dispose();
            snapshot = new Bitmap(panel1.ClientRectangle.Width, panel1.ClientRectangle.Height);
            using (Graphics g = Graphics.FromImage(snapshot))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                drawRink(g);
                g.SmoothingMode = SmoothingMode.None;
                foreach (drawObj o in objs)
                {
                    o.draw(g, selected == null ? false : selected.Equals(o));
                }
            }
            panel1.Invalidate();
            panel1.Update();
        }

        private void drawRink(Graphics g)
        {
            using (Pen blackPen = new Pen(Color.Black, 2))
            using (Pen redPen = new Pen(Color.Red, 2))
            using (Pen bluePen = new Pen(Color.Blue, 2))
            using (SolidBrush redBrush = new SolidBrush(Color.Red))
            using (SolidBrush blueBrush = new SolidBrush(Color.Blue))
            {
                /**  Lines  **/
                g.DrawLine(bluePen, (rinkWidth / 2) - curSpecs.BlueLineFromCenter, 0, (rinkWidth / 2) - curSpecs.BlueLineFromCenter, rinkHeight); //Left blue line
                g.DrawLine(bluePen, (rinkWidth / 2) + curSpecs.BlueLineFromCenter, 0, (rinkWidth / 2) + curSpecs.BlueLineFromCenter, rinkHeight); //Right blue line
                Pen centerPen = new Pen(Color.Red, 5);
                centerPen.DashCap = DashCap.Round;
                centerPen.DashStyle = DashStyle.Dash;
                centerPen.DashOffset = 1;
                g.DrawLine(centerPen, (rinkWidth / 2), 0, (rinkWidth / 2), rinkHeight); //Centerline
                centerPen.Dispose();
                g.DrawLine(redPen, 65, 8, 65, rinkHeight - 8); //Left red line
                g.DrawLine(redPen, rinkWidth - 66, 8, rinkWidth - 66, rinkHeight - 8); //Right red line
                                                                                       /**  Nets  **/
                g.DrawLine(blackPen, 65 - 20, (rinkHeight / 2) - 15, 64, (rinkHeight / 2) - 15); //Left top
                g.DrawLine(blackPen, 65 - 20, (rinkHeight / 2) + 15, 64, (rinkHeight / 2) + 15); //Left bottom
                g.DrawLine(blackPen, 65 - 20, (rinkHeight / 2) - 15, 65 - 20, (rinkHeight / 2) + 15); //Left side
                g.FillPie(Brushes.PaleTurquoise, 66 - 40, (rinkHeight / 2) - 40, 80, 80, 270, 180); //Left arc

                g.DrawLine(blackPen, rinkWidth - 65 + 20, (rinkHeight / 2) - 15, rinkWidth - 65, (rinkHeight / 2) - 15); //Right
                g.DrawLine(blackPen, rinkWidth - 65 + 20, (rinkHeight / 2) + 15, rinkWidth - 65, (rinkHeight / 2) + 15); //Right
                g.DrawLine(blackPen, rinkWidth - 65 + 20, (rinkHeight / 2) - 15, rinkWidth - 65 + 20, (rinkHeight / 2) + 15); //Right
                g.FillPie(Brushes.PaleTurquoise, rinkWidth - 67 - 40, (rinkHeight / 2) - 40, 80, 80, 90, 180); //Right arc
                                                                                                               /**  Black border  **/
                g.DrawLine(blackPen, 100, 0, rinkWidth - 100, 0); //Top black line
                g.DrawLine(blackPen, 1, 100, 1, rinkHeight - 100); //Left black line
                g.DrawLine(blackPen, rinkWidth - 1, 100, rinkWidth - 1, rinkHeight - 100); //Right black line
                g.DrawLine(blackPen, 100, rinkHeight - 1, rinkWidth - 100, rinkHeight - 1); //Bottom black line
                g.DrawArc(blackPen, 0, 0, 200, 200, 180, 90); //Top left curve
                g.DrawArc(blackPen, 0, rinkHeight - 201, 200, 200, 180, -90); //Bottom left curve
                g.DrawArc(blackPen, rinkWidth - 201, 0, 200, 200, 270, 90); //Top right curve
                g.DrawArc(blackPen, rinkWidth - 201, rinkHeight - 201, 200, 200, 90, -90); //Bottom right curve
                                                                                           /**  Dots  **/
                g.FillEllipse(blueBrush, (rinkWidth / 2) - curSpecs.DotRadius, (rinkHeight / 2) - curSpecs.DotRadius, curSpecs.DotRadius * 2, curSpecs.DotRadius * 2); //Center dot
                g.FillEllipse(redBrush, curSpecs.EdgeToCircle - curSpecs.DotRadius, (rinkHeight / 2) - curSpecs.CenterNetToCircle - curSpecs.DotRadius, curSpecs.DotRadius * 2, curSpecs.DotRadius * 2); //Top left
                g.FillEllipse(redBrush, rinkWidth - curSpecs.EdgeToCircle - curSpecs.DotRadius, (rinkHeight / 2) - curSpecs.CenterNetToCircle - curSpecs.DotRadius, curSpecs.DotRadius * 2, curSpecs.DotRadius * 2); //Top right circle
                g.FillEllipse(redBrush, curSpecs.EdgeToCircle - curSpecs.DotRadius, (rinkHeight / 2) + curSpecs.CenterNetToCircle - curSpecs.DotRadius, curSpecs.DotRadius * 2, curSpecs.DotRadius * 2); //Bottom left circle
                g.FillEllipse(redBrush, rinkWidth - curSpecs.EdgeToCircle - curSpecs.DotRadius, (rinkHeight / 2) + curSpecs.CenterNetToCircle - curSpecs.DotRadius, curSpecs.DotRadius * 2, curSpecs.DotRadius * 2); //Bottom right circle
                g.FillEllipse(redBrush, (rinkWidth / 2) - curSpecs.AuxDotsFromCenter - curSpecs.DotRadius, (rinkHeight / 2) - curSpecs.CenterNetToCircle - curSpecs.DotRadius, curSpecs.DotRadius * 2, curSpecs.DotRadius * 2);
                g.FillEllipse(redBrush, (rinkWidth / 2) + curSpecs.AuxDotsFromCenter - curSpecs.DotRadius, (rinkHeight / 2) - curSpecs.CenterNetToCircle - curSpecs.DotRadius, curSpecs.DotRadius * 2, curSpecs.DotRadius * 2);
                g.FillEllipse(redBrush, (rinkWidth / 2) - curSpecs.AuxDotsFromCenter - curSpecs.DotRadius, (rinkHeight / 2) + curSpecs.CenterNetToCircle - curSpecs.DotRadius, curSpecs.DotRadius * 2, curSpecs.DotRadius * 2);
                g.FillEllipse(redBrush, (rinkWidth / 2) + curSpecs.AuxDotsFromCenter - curSpecs.DotRadius, (rinkHeight / 2) + curSpecs.CenterNetToCircle - curSpecs.DotRadius, curSpecs.DotRadius * 2, curSpecs.DotRadius * 2);

                /**  Circles  **/
                g.DrawEllipse(redPen, curSpecs.EdgeToCircle - curSpecs.CircleRadius, (rinkHeight / 2) - curSpecs.CenterNetToCircle - curSpecs.CircleRadius, curSpecs.CircleRadius * 2, curSpecs.CircleRadius * 2); //Top left
                g.DrawEllipse(redPen, rinkWidth - curSpecs.EdgeToCircle - curSpecs.CircleRadius, (rinkHeight / 2) - curSpecs.CenterNetToCircle - curSpecs.CircleRadius, curSpecs.CircleRadius * 2, curSpecs.CircleRadius * 2); //Top right
                g.DrawEllipse(redPen, curSpecs.EdgeToCircle - curSpecs.CircleRadius, (rinkHeight / 2) + curSpecs.CenterNetToCircle - curSpecs.CircleRadius, curSpecs.CircleRadius * 2, curSpecs.CircleRadius * 2); //Bottom left
                g.DrawEllipse(redPen, rinkWidth - curSpecs.EdgeToCircle - curSpecs.CircleRadius, (rinkHeight / 2) + curSpecs.CenterNetToCircle - curSpecs.CircleRadius, curSpecs.CircleRadius * 2, curSpecs.CircleRadius * 2); //Bottom right
                g.DrawEllipse(bluePen, (rinkWidth / 2) - curSpecs.CircleRadius, (rinkHeight / 2) - curSpecs.CircleRadius, curSpecs.CircleRadius * 2, curSpecs.CircleRadius * 2);
            }
        }

        #region Menu Bar
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            Line.Checked = false;
            Pencil.Checked = false;
            ToolStripButton btnClicked = sender as ToolStripButton;
            btnClicked.Checked = true;
            selectedTool = btnClicked.Name;
        }

        private void colorLabel_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                if (colorDialog1.Color.IsNamedColor) colorLabel.Text = colorDialog1.Color.Name;
                else
                {
                    int x = colorDialog1.Color.ToArgb();
                    colorLabel.Text = "#" + (x >> 16 & 0xFF).ToString("X2") + (x >> 8 & 0xFF).ToString("X2") + (x & 0xFF).ToString("X2");
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
            StringCollection sc = CoachDraw.Properties.Settings.Default.recentFiles;
            if (sc == null) sc = new StringCollection();
            if (!sc.Contains(path))
            {
                sc.Add(path);
                if (sc.Count > 5) sc.RemoveAt(0);
                updateRecentFiles();
            }
        }

        private void updateRecentFiles()
        {
            StringCollection sc = Properties.Settings.Default.recentFiles;
            if (sc != null && sc.Count > 0)
            {
                for (int i = 0; i < sc.Count; i++)
                {
                    string path = sc[i];
                    if (!File.Exists(path))
                    {
                        sc.RemoveAt(i--);
                        continue;
                    }
                    ToolStripMenuItem tempItem = new ToolStripMenuItem(compactString(path, 50), null, new EventHandler(this.clickRecentFile));
                    tempItem.Tag = path;
                    recentFilesToolStripMenuItem.DropDownItems.Add(tempItem);
                }
                Properties.Settings.Default.recentFiles = sc;
                Properties.Settings.Default.Save();
                if (sc.Count > 0) recentFilesToolStripMenuItem.Enabled = true;
                else recentFilesToolStripMenuItem.Enabled = false;
            }
            else
                recentFilesToolStripMenuItem.Enabled = false;
        }

        private void clickRecentFile(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
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
            currentFile = "";
            saved = true;
            objs.Clear();
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
            objs.Clear();
            redraw();
            PlayInfo result = null;
            if (path.ToUpper().EndsWith(".PLY"))
                result = Plays.LoadPLYFile(path, ref objs);
            else if (path.ToUpper().EndsWith(".PLYX"))
            {
                result = Plays.LoadPLYXFile(path, ref objs);
                currentFile = path;
                saved = true;
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
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "All supported files (*.ply, *.plyx)|*.ply;*.plyx|New play files (*.plyx)|*.plyx|HockeyVision play files (*.ply)|*.ply|All files (*.*)|*.*";
            dlg.FilterIndex = 2;
            if (dlg.ShowDialog() == DialogResult.OK)
                openFile(dlg.FileName, true);
        }

        private bool checkSaved(string operation)
        {
            if (!saved)
            {
                DialogResult d = MessageBox.Show("The current play is not saved. Do you want to save before " + operation + "?", "Unsaved Play", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button3);
                if (d == DialogResult.Yes)
                    saveToolStripMenuItem_Click(saveToolStripMenuItem, null);
                else if (d == DialogResult.Cancel)
                    return false;
            }
            return true;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentFile != "")
            {
                if (Plays.savePLYXFile(currentFile, objs, txtPlayName.Text, txtPlayDesc.Text, plyxVersion))
                {
                    saved = true;
                    updateTitlebar();
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (checkSaved("exiting"))
                Application.Exit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!checkSaved("exiting")) e.Cancel = true;
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
                if (ld.ShowDialog() == DialogResult.Yes && Plays.savePLYXFile(ld.fileName, objs, ld.playName, txtPlayDesc.Text, plyxVersion))
                {
                    txtPlayName.Text = ld.playName;
                    saved = true;
                    currentFile = ld.fileName;
                    addRecentFile(ld.fileName);
                    updateTitlebar();
                }
            }
        }

        private Font ShrinkFont(Graphics g, String text, Font font, Size proposedSize)
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
            PrintDocument pd = new PrintDocument();
            pd.DefaultPageSettings.Landscape = true;
            pd.PrintPage += (object printSender, PrintPageEventArgs pe) =>
            {
                float posy = 50;
                Font nameFont = new Font("Helvetica", 14.0f, FontStyle.Bold);
                SizeF nameSize = pe.Graphics.MeasureString(txtPlayName.Text, nameFont);
                pe.Graphics.DrawString(txtPlayName.Text, nameFont, new SolidBrush(Color.Black), new RectangleF(50.0f, posy, 900.0f, nameSize.Height), new StringFormat() { Alignment = StringAlignment.Center });
                posy += nameSize.Height;
                pe.Graphics.DrawImage(snapshot, new Rectangle(50, (int)posy, pe.PageSettings.PaperSize.Height - 100, Convert.ToInt32(snapshot.Height * ((pe.PageSettings.PaperSize.Height - 100.0) / snapshot.Width))));
                posy += snapshot.Height;
                Font descFont = ShrinkFont(pe.Graphics, txtPlayDesc.Text, new Font("Helvetica", 10.0f), new Size(900, 750 - (int)posy));
                SizeF descSize = pe.Graphics.MeasureString(txtPlayDesc.Text, descFont);
                pe.Graphics.DrawString(txtPlayDesc.Text, descFont, new SolidBrush(Color.Black), new RectangleF(50.0f, posy, 900.0f, 750.0f - posy));
                pe.Graphics.DrawString("CoachDraw © 2017", new Font("Helvetica", 5.0f), new SolidBrush(Color.Black), 50, 825.0f);
            };
            PrintDialog dialog = new PrintDialog();
            dialog.Document = pd;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                PrintPreviewDialog pp = new PrintPreviewDialog();
                pp.Document = pd;
                pp.ShowDialog();
                //pd.PrinterSettings = dialog.PrinterSettings;
                //pd.Print();
            }
        }

        private void printMultiple_Click(object sender, EventArgs e)
        {
            using (frmMultiPrint mp = new frmMultiPrint(currentFile, lastSelectedMultiPrint, lastSetMultiPrint))
            {
                if (mp.ShowDialog() == DialogResult.Yes)
                {
                    PrintMultiplePlays(mp.plays);
                }
                lastSelectedMultiPrint = mp.lastSelected;
                lastSetMultiPrint = mp.plays ?? new List<string>() { "", "", "", "" };
            }
        }

        private void PrintMultiplePlays(List<string> plays)
        {
            PrintDocument pd = new PrintDocument();
            pd.PrintPage += (object printSender, PrintPageEventArgs pe) =>
            {
                float posy = 50;
                foreach (string play in plays)
                {
                    if (play == "")
                    {
                        posy += 250.0f;
                        continue;
                    }
                    List<drawObj> tempObjs = new List<drawObj>();
                    PlayInfo result = Plays.LoadPLYXFile(play, ref tempObjs);
                    Bitmap rink = new Bitmap(1000, 500);
                    using (Graphics g = Graphics.FromImage(rink))
                    {
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        drawRink(g);
                        g.SmoothingMode = SmoothingMode.None;
                        foreach (drawObj o in tempObjs)
                        {
                            o.draw(g, false);
                        }
                    }
                    pe.Graphics.DrawLine(new Pen(Color.Black, 1.0f), 0.0f, posy, 1100.0f, posy);
                    posy += 5.0f;
                    int newWidth = (int)((240.0 / rink.Height) * rink.Width);
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
            PrintDialog dialog = new PrintDialog();
            dialog.Document = pd;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                PrintPreviewDialog pp = new PrintPreviewDialog();
                pp.Document = pd;
                pp.Height = this.Height;
                pp.Width = this.Width;
                pp.ShowDialog();
                //pd.PrinterSettings = dialog.PrinterSettings;
                //pd.Print();
            }
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
        int curScale = 1;

        public RinkSpecs(int newScale)
        {
            setScale(newScale);
        }

        public void setScale(int newScale)
        {
            BlueLineFromCenter /= curScale; BlueLineFromCenter *= newScale;
            AuxDotsFromCenter /= curScale; AuxDotsFromCenter *= newScale;
            RedToBlue /= curScale; RedToBlue *= newScale;
            EdgeToRed /= curScale; EdgeToRed *= newScale;
            CenterNetToCircle /= curScale; CenterNetToCircle *= newScale;
            RedToCircle /= curScale; RedToCircle *= newScale;
            EdgeToCircle /= curScale; EdgeToCircle *= newScale;
            NetWidth /= curScale; NetWidth *= newScale;
            NetLength /= curScale; NetLength *= newScale;
            NetArcRadius /= curScale; NetArcRadius *= newScale;
            CircleRadius /= curScale; CircleRadius *= newScale;
            curScale = newScale;
        }
    }
}
