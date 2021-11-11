using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace CoachDraw
{
    public enum ItemType : byte
    {
        Offensive,
        Defensive,
        Winger,
        Center,
        Defenseman,
        Pylon,
        Puck,
        Pucks,
        Coach,
        None,
        DefensiveInt,
        PlayerNumber,
        Goalie
    }
    public enum LineType : byte
    {
        Forward,
        Backward,
        CarryingPuck,
        Pass,
        Shot,
        Lateral
    }
    public enum EndType : byte
    {
        None,
        Arrow,
        PlayTheMan,
        Stop,
        WideArrow
    }

    public class DrawObj
    {
        public ItemType objType = ItemType.None;
        public Line objLine;
        public int objLabel = -1;
        public Point objLoc;
        public Color color = Color.Blue;
        public Region hitBox;

        private void setHitBox(Rectangle box)
        {
            hitBox = new Region(box);
        }

        private Color invertColor(Color c)
        {
            return Color.FromArgb(255 - c.R, 255 - c.G, 255 - c.B);
        }

        public void Draw(Graphics g, bool selected)
        {
            var myPen = selected ? new Pen(invertColor(color), 1) : new Pen(color, 1);
            var drawString = "";
            var fontSize = 12;
            var lineSkip = new Rectangle();
            switch (objType)
            {
                case ItemType.Offensive: drawString = "O"; break;
                case ItemType.Defensive: drawString = "X"; break;
                case ItemType.Defenseman: drawString = "D"; break;
                case ItemType.DefensiveInt: drawString = "Δ"; fontSize = 16; break;
                case ItemType.Goalie: drawString = "G"; break;
                case ItemType.Coach: drawString = "©"; fontSize = 16; break;
                case ItemType.Center: drawString = "C"; break;
                case ItemType.Winger: drawString = "W"; break;
                case ItemType.Pylon:
                    g.FillPolygon(new SolidBrush(color), new[]
                    {
                        new Point(objLoc.X - 6, objLoc.Y + 6),
                        new Point(objLoc.X + 6, objLoc.Y + 6),
                        new Point(objLoc.X, objLoc.Y - 6)
                    });
                    setHitBox(new Rectangle(objLoc.X - 6, objLoc.Y - 6, 12, 12));
                    break;
                case ItemType.Puck:
                    g.FillRectangle(new SolidBrush(color), objLoc.X - 4, objLoc.Y - 2, 8, 4);
                    setHitBox(new Rectangle(objLoc.X - 4, objLoc.Y - 2, 8, 4));
                    break;
                case ItemType.Pucks:
                    g.FillEllipse(new SolidBrush(color), objLoc.X - 2, objLoc.Y - 2, 4, 4);
                    g.FillEllipse(new SolidBrush(color), objLoc.X - 6, objLoc.Y - 6, 4, 4);
                    g.FillEllipse(new SolidBrush(color), objLoc.X + 6, objLoc.Y + 6, 4, 4);
                    g.FillEllipse(new SolidBrush(color), objLoc.X - 6, objLoc.Y + 6, 4, 4);
                    g.FillEllipse(new SolidBrush(color), objLoc.X + 6, objLoc.Y - 6, 4, 4);
                    setHitBox(new Rectangle(objLoc.X - 6, objLoc.Y - 6, 12, 12));
                    break;
                case ItemType.None:
                case ItemType.PlayerNumber:
                    break;
                default:
                    Debugger.Break();
                    break;
            }
            if (objLabel >= 0) drawString += objLabel;
            if (drawString != "")
            {
                var fSize = g.MeasureString(drawString, new Font("MS Sans Serif", fontSize));
                var x = objLoc.X - (int)(fSize.Width / 2);
                var y = objLoc.Y - (int)(fSize.Height / 2);
                lineSkip = new Rectangle(x, y, (int)fSize.Width, (int)fSize.Height);
                setHitBox(lineSkip);
                g.DrawString(drawString, new Font("MS Sans Serif", fontSize), new SolidBrush(myPen.Color), x, y);
            }
            if (objLine != null)
                if (!objLine.draw(g, selected, lineSkip))
                    objLine = null; // line was actually invalid, kill it
        }
    }


    public class Line
    {
        public LineType lineType = LineType.Forward; //Forward = 0, Backward = 1, CarryingPuck = 2, Pass = 3, Shot = 4, Lateral = 5
        public EndType endType = EndType.None; //None = 0, Arrow = 1, Man = 2, Stop = 3
        public byte lineWidth = 2;
        public List<Point> points;
        public Color color;
        public Region hitBox;
        public bool smoothed;

        public Line()
        {
            points = new List<Point>();
            color = Color.Black;
        }

        private void setHitBox()
        {
            var gp = new GraphicsPath();
            gp.AddLines(points.ToArray());
            gp.Widen(new Pen(Color.Black, 10));
            hitBox = new Region(gp);
        }

        public int getAggregateLength(int start, int end)
        {
            var returnVal = 0;
            for (var i = start; i < end; i++)
                returnVal += Smoothing.GetLineLength(points[i], points[i + 1]);
            return returnVal;
        }

        public void cleanDuplicates()
        {
            for (var i = 1; i < points.Count; i++)
            {
                if (points[i].X == points[i - 1].X && points[i].Y == points[i - 1].Y)
                    points.RemoveAt(i--);
            }
        }

        [SuppressMessage("ReSharper", "ArrangeRedundantParentheses")]
        public bool draw(Graphics g, bool selected, Rectangle skipBox)
        {
            var myPen = selected ? new Pen(Color.FromArgb(255 - color.R, 255 - color.G, 255 - color.B), lineWidth) : new Pen(color, lineWidth);
            var cappedPen = (Pen)myPen.Clone();
            if (!smoothed && lineType != LineType.Lateral && points.Count >= 20)
            {
                //points = Smoothing.DouglasPeuckerReduction(points, 20.0);
                //points = Smoothing.McMasters(points);
                points = Smoothing.BasicReduction(points, 5);
                smoothed = true;
            }
            if (getAggregateLength(0, points.Count - 1) < 5) return false; // Line doesn't actually seem to go anywhere. return false so that sucker can be removed!
            setHitBox();

            var end_path = new GraphicsPath();
            switch (endType)
            {
                case EndType.Arrow:
                case EndType.WideArrow:
                    var neww = endType == EndType.WideArrow ? 14 : 7;
                    var newx = neww * Math.Cos(Math.PI / 3);
                    var newy = neww * Math.Sin(Math.PI / 3);
                    end_path.AddLine(0, 0, (float)newx * -1, (float)newy * -1);
                    end_path.AddLine(0, 0, (float)newx, (float)newy * -1);
                    break;
                case EndType.PlayTheMan:
                    end_path.AddArc(-5, 0, 10, 10, 180, 180);
                    break;
                case EndType.Stop:
                    end_path.AddLine(-5, 3, 5, 3);
                    break;
            }
            cappedPen.CustomEndCap = new CustomLineCap(null, end_path);

            switch (lineType)
            {
                case LineType.Forward:
                    if (points.Count > 2)
                        g.DrawCurve(cappedPen, points.ToArray(), 0, points.Count - 2, 0.5F);
                    else
                        g.DrawCurve(cappedPen, points.ToArray(), 0, points.Count - 1, 0.5F);
                    break;
                case LineType.Shot:
                    var origin = points[points.Count - 1];
                    var first = points[0];
                    var r = 8;
                    var a2 = Math.Atan2(first.Y - origin.Y, first.X - origin.X);
                    using (var newPen = new Pen(color, lineWidth))
                    {
                        var move = new Matrix();
                        move.Translate(4, 8);
                        end_path.Transform(move);
                        newPen.CustomEndCap = new CustomLineCap(null, end_path);
                        g.DrawLine(myPen, (int)(r * Math.Cos(a2 + Math.PI / 2)) + first.X, (int)(r * Math.Sin(a2 + Math.PI / 2)) + first.Y,
                            (int)(8 / Math.Sin(Math.PI / 6) * Math.Cos(a2 + Math.PI / 6)) + origin.X, (int)(8 / Math.Sin(Math.PI / 6) * Math.Sin(a2 + Math.PI / 6)) + origin.Y);
                        g.DrawLine(newPen, (int)(r * Math.Cos(a2 - Math.PI / 2)) + first.X, (int)(r * Math.Sin(a2 - Math.PI / 2)) + first.Y,
                            (int)(8 / Math.Sin(Math.PI / 6) * Math.Cos(a2 - Math.PI / 6)) + origin.X, (int)(8 / Math.Sin(Math.PI / 6) * Math.Sin(a2 - Math.PI / 6)) + origin.Y);
                    }
                    break;
                case LineType.Pass:
                    using (var newPen = new Pen(color, lineWidth))
                    {
                        newPen.DashStyle = DashStyle.Dash;
                        newPen.CustomEndCap = new CustomLineCap(null, end_path);
                        g.DrawCurve(newPen, points.ToArray());
                    }
                    break;
                case LineType.Lateral:
                    if (points.Count == 2)
                    {
                        var firstLine = Smoothing.GetPointFromDistance(points[0], points[1], 30);
                        var secondLine = Smoothing.GetPointFromDistance(points[1], points[0], 30);
                        g.DrawLine(myPen, points[0], firstLine);
                        g.DrawLine(cappedPen, secondLine, points[1]);
                        var angle = Smoothing.getAngle(points[0], points[1]) + 90;
                        while (Smoothing.GetLineLength(firstLine, secondLine) > 30)
                        {
                            var nextPoint = Smoothing.GetPointFromDistance(firstLine, points[1], 25);
                            g.TranslateTransform(nextPoint.X, nextPoint.Y);
                            g.RotateTransform(angle);
                            g.DrawLine(myPen, new Point(-7, 0), new Point(7, 0));
                            g.ResetTransform();
                            firstLine = nextPoint;
                        }
                    }
                    break;
                case LineType.CarryingPuck:
                    if (points.Count == 2)
                    {
                        var lineLength = Smoothing.GetLineLength(points[0], points[1]);
                        if (lineLength < 20) // turns out this line is messed up and shouldn't exist, probably a remnant from a bad PLY file
                            return false;
                        var cpoints = new PointF[lineLength - 15];
                        var angle = Smoothing.getAngle(points[0], points[1]);
                        for (var i = 0; i < lineLength - 15; i++)
                        {
                            cpoints[i] = new PointF
                            {
                                X = i,
                                Y = -1 * (float)(Math.Sin(10 * Math.PI * i / 200) * 10)
                            };
                            if (Math.Round(cpoints[i].Y, 1) == 0 && lineLength - 15 < i + 20)
                                break;
                        }
                        g.TranslateTransform(points[0].X, points[0].Y);
                        g.RotateTransform(angle);
                        g.DrawLines(myPen, cpoints);
                        g.ResetTransform();
                        g.DrawLine(cappedPen, points[0], points[1]);
                    }
                    else
                    {
                        g.DrawCurve(cappedPen, points.ToArray());
                        var cpoints = new List<PointF>();
                        var start = 0;
                        var bottom = false;
                        for (var i = 0; i < points.Count; i++)
                        {
                            var length = Smoothing.GetLineLength(points[start], points[i]);
                            if (length <= 20) continue;
                            {
                                var angle2 = (Math.PI / 2) - Math.Atan2(points[i].Y - points[start].Y, points[i].X - points[start].X);
                                var x = (int)Math.Round(Math.Sin(angle2) * 20, MidpointRounding.AwayFromZero);
                                var y = (int)Math.Round(Math.Cos(angle2) * 20, MidpointRounding.AwayFromZero);
                                var newPoint = new Point(points[start].X + x, points[start].Y + y);
                                length = Smoothing.GetLineLength(points[start], newPoint);
                                points.Insert(i, newPoint);
                            }
                            if (getAggregateLength(i, points.Count - 1) < 20) break;

                            var angle = Smoothing.getAngle(points[start], points[i]);

                            for (var j = 0; j <= length; j++)
                            {
                                var newPoint = new PointF
                                {
                                    X = j,
                                    Y = -1 * (float)(Math.Sin(10 * Math.PI * (bottom ? j + 20 : j) / 200) * 10)
                                };
                                cpoints.Add(newPoint);
                            }
                            g.TranslateTransform(points[start].X, points[start].Y);
                            g.RotateTransform(angle);
                            g.DrawLines(myPen, cpoints.ToArray());
                            g.ResetTransform();
                            cpoints.Clear();
                            start = i;
                            bottom = !bottom;
                        }
                    }
                    break;
                case LineType.Backward:
                    if (points.Count == 2)
                    {
                        var lineLength = Smoothing.GetLineLength(points[0], points[1]);
                        var cpoints = new List<PointF>();
                        var angle = (float)(Math.Atan2(points[1].Y - points[0].Y, points[1].X - points[0].X) * 180 / Math.PI);
                        g.TranslateTransform(points[0].X, points[0].Y);
                        g.RotateTransform(angle);
                        for (var i = 0; i < lineLength - 15; i++)
                        {
                            var point = new PointF
                            {
                                X = i,
                                Y = -1 * (float)(Math.Sin(10 * Math.PI * i / 200) * 10)
                            };
                            if (Math.Round(point.Y, 1) == 0 && lineLength - 15 < i + 20)
                            {
                                g.DrawLine(cappedPen, point.X, 0, lineLength, 0);
                                break;
                            }
                            cpoints.Add(point);
                            if (point.Y > -4 && point.Y < 4)
                            {
                                if (cpoints.Count > 1) g.DrawLines(myPen, cpoints.ToArray());
                                cpoints.Clear();
                            }
                        }
                        g.ResetTransform();
                    }
                    else
                    {
                        var cpoints = new List<PointF>();
                        var start = 0;
                        var bottom = false;
                        for (var i = 0; i < points.Count; i++)
                        {
                            var length = Smoothing.GetLineLength(points[start], points[i]);
                            if (length < 20) continue;

                            if (length > 20)
                            {
                                var eh = points[start];
                                var meh = points[i];
                                var angle2 = (Math.PI / 2) - Math.Atan2(meh.Y - eh.Y, meh.X - eh.X);
                                var x = (int)Math.Round(Math.Sin(angle2) * 20, MidpointRounding.AwayFromZero);
                                var y = (int)Math.Round(Math.Cos(angle2) * 20, MidpointRounding.AwayFromZero);
                                var bleh = new Point(eh.X + x, eh.Y + y);
                                length = Smoothing.GetLineLength(eh, bleh);
                                points.Insert(i, bleh);
                            }
                            if (getAggregateLength(i, points.Count - 1) < 20) break;

                            var angle = (float)(Math.Atan2(points[i].Y - points[start].Y, points[i].X - points[start].X) * 180 / Math.PI);
                            g.TranslateTransform(points[start].X, points[start].Y);
                            g.RotateTransform(angle);
                            for (var j = 0; j < length; j++)
                            {
                                var newPoint = new PointF
                                {
                                    X = j,
                                    Y = -1 * (float)(Math.Sin(10 * Math.PI * (bottom ? j + 20 : j) / 200) * 10)
                                };
                                cpoints.Add(newPoint);
                                if (newPoint.Y > -4 && newPoint.Y < 4)
                                {
                                    if (cpoints.Count > 1) g.DrawLines(myPen, cpoints.ToArray());
                                    cpoints.Clear();
                                }
                            }
                            g.ResetTransform();
                            cpoints.Clear();
                            start = i;
                            bottom = !bottom;
                        }
                    }
                    break;
                default:
                    Debugger.Break();
                    break;
            }
            myPen.Dispose();
            return true;
        }
    }
}
