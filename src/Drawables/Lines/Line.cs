using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using CoachDraw.Extensions;

namespace CoachDraw.Drawables.Lines
{
    public sealed class Line
    {
        /// <summary>
        /// Forward = 0, Backward = 1, CarryingPuck = 2, Pass = 3, Shot = 4, Lateral = 5
        /// </summary>
        public LineType LineType { get; set; } = LineType.Forward;
        /// <summary>
        /// None = 0, Arrow = 1, Man = 2, Stop = 3
        /// </summary>
        public EndType EndType { get; set; } = EndType.None;
        public byte LineWidth { get; set; } = 2;
        public List<Point> Points { get; set; } = new();
        public Color Color { get; set; } = Color.Black;
        public Region HitBox { get; private set; }
        public bool Smoothed { get; set; }

        private void SetHitBox()
        {
            var gp = new GraphicsPath();
            gp.AddLines(Points.ToArray());
            gp.Widen(new Pen(Color.Black, 10));
            HitBox = new Region(gp);
        }

        public int GetAggregateLength(int start, int end)
        {
            var returnVal = 0;
            for (var i = start; i < end; i++)
                returnVal += Smoothing.GetLineLength(Points[i], Points[i + 1]);
            return returnVal;
        }

        public void CleanDuplicates()
        {
            for (var i = 1; i < Points.Count; i++)
            {
                if (Points[i].X == Points[i - 1].X && Points[i].Y == Points[i - 1].Y)
                    Points.RemoveAt(i--);
            }
        }

        public bool Draw(Graphics g, bool selected, Rectangle skipBox)
        {
            using var myPen = selected ? new Pen(Color.Invert(), LineWidth) : new Pen(Color, LineWidth);
            using var cappedPen = new Pen(myPen.Color, myPen.Width);
            if (!Smoothed && LineType != LineType.Lateral && Points.Count >= 20)
            {
                //points = Smoothing.DouglasPeuckerReduction(points, 20.0);
                //points = Smoothing.McMasters(points);
                Points = Smoothing.BasicReduction(Points, 5);
                Smoothed = true;
            }
            if (GetAggregateLength(0, Points.Count - 1) < 5)
                return false; // Line doesn't actually seem to go anywhere. return false so that sucker can be removed!
            SetHitBox();

            var endPath = new GraphicsPath();
            switch (EndType)
            {
                case EndType.Arrow:
                case EndType.WideArrow:
                    var neww = EndType == EndType.WideArrow ? 14 : 7;
                    var newx = neww * Math.Cos(Math.PI / 3);
                    var newy = neww * Math.Sin(Math.PI / 3);
                    endPath.AddLine(0, 0, (float)newx * -1, (float)newy * -1);
                    endPath.AddLine(0, 0, (float)newx, (float)newy * -1);
                    break;
                case EndType.PlayTheMan:
                    endPath.AddArc(-5, 0, 10, 10, 180, 180);
                    break;
                case EndType.Stop:
                    endPath.AddLine(-5, 3, 5, 3);
                    break;
            }
            cappedPen.CustomEndCap = new CustomLineCap(null, endPath);

            switch (LineType)
            {
                case LineType.Forward:
                    if (Points.Count > 2)
                        g.DrawCurve(cappedPen, Points.ToArray(), 0, Points.Count - 2, 0.5F);
                    else
                        g.DrawCurve(cappedPen, Points.ToArray(), 0, Points.Count - 1, 0.5F);
                    break;
                case LineType.Shot:
                    var origin = Points[^1];
                    var first = Points[0];
                    const int r = 8;
                    var a2 = Math.Atan2(first.Y - origin.Y, first.X - origin.X);
                    using (var newPen = new Pen(Color, LineWidth))
                    {
                        var move = new Matrix();
                        move.Translate(4, 8);
                        endPath.Transform(move);
                        newPen.CustomEndCap = new CustomLineCap(null, endPath);
                        g.DrawLine(
                            myPen,
                            (int)(r * Math.Cos(a2 + Math.PI / 2)) + first.X,
                            (int)(r * Math.Sin(a2 + Math.PI / 2)) + first.Y,
                            (int)(8 / Math.Sin(Math.PI / 6) * Math.Cos(a2 + Math.PI / 6)) + origin.X,
                            (int)(8 / Math.Sin(Math.PI / 6) * Math.Sin(a2 + Math.PI / 6)) + origin.Y
                        );
                        g.DrawLine(
                            newPen,
                            (int)(r * Math.Cos(a2 - Math.PI / 2)) + first.X,
                            (int)(r * Math.Sin(a2 - Math.PI / 2)) + first.Y,
                            (int)(8 / Math.Sin(Math.PI / 6) * Math.Cos(a2 - Math.PI / 6)) + origin.X,
                            (int)(8 / Math.Sin(Math.PI / 6) * Math.Sin(a2 - Math.PI / 6)) + origin.Y
                        );
                    }
                    break;
                case LineType.Pass:
                    using (var newPen = new Pen(Color, LineWidth))
                    {
                        newPen.DashStyle = DashStyle.Dash;
                        newPen.CustomEndCap = new CustomLineCap(null, endPath);
                        g.DrawCurve(newPen, Points.ToArray());
                    }
                    break;
                case LineType.Lateral:
                    if (Points.Count == 2)
                    {
                        var firstLine = Smoothing.GetPointFromDistance(Points[0], Points[1], 30);
                        var secondLine = Smoothing.GetPointFromDistance(Points[1], Points[0], 30);
                        g.DrawLine(myPen, Points[0], firstLine);
                        g.DrawLine(cappedPen, secondLine, Points[1]);
                        var angle = Smoothing.GetAngle(Points[0], Points[1]) + 90;
                        while (Smoothing.GetLineLength(firstLine, secondLine) > 30)
                        {
                            var nextPoint = Smoothing.GetPointFromDistance(firstLine, Points[1], 25);
                            g.TranslateTransform(nextPoint.X, nextPoint.Y);
                            g.RotateTransform(angle);
                            g.DrawLine(myPen, new Point(-7, 0), new Point(7, 0));
                            g.ResetTransform();
                            firstLine = nextPoint;
                        }
                    }
                    break;
                case LineType.CarryingPuck:
                    if (Points.Count == 2)
                    {
                        var lineLength = Smoothing.GetLineLength(Points[0], Points[1]);
                        if (lineLength < 20) // turns out this line is messed up and shouldn't exist, probably a remnant from a bad PLY file
                            return false;
                        var cpoints = new PointF[lineLength - 15];
                        var angle = Smoothing.GetAngle(Points[0], Points[1]);
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
                        g.TranslateTransform(Points[0].X, Points[0].Y);
                        g.RotateTransform(angle);
                        g.DrawLines(myPen, cpoints);
                        g.ResetTransform();
                        g.DrawLine(cappedPen, Points[0], Points[1]);
                    }
                    else
                    {
                        g.DrawCurve(cappedPen, Points.ToArray());
                        var cpoints = new List<PointF>();
                        var start = 0;
                        var bottom = false;
                        for (var i = 0; i < Points.Count; i++)
                        {
                            var length = Smoothing.GetLineLength(Points[start], Points[i]);
                            if (length <= 20) continue;
                            {
                                var angle2 = (Math.PI / 2) - Math.Atan2(Points[i].Y - Points[start].Y, Points[i].X - Points[start].X);
                                var x = (int)Math.Round(Math.Sin(angle2) * 20, MidpointRounding.AwayFromZero);
                                var y = (int)Math.Round(Math.Cos(angle2) * 20, MidpointRounding.AwayFromZero);
                                var newPoint = new Point(Points[start].X + x, Points[start].Y + y);
                                length = Smoothing.GetLineLength(Points[start], newPoint);
                                Points.Insert(i, newPoint);
                            }
                            if (GetAggregateLength(i, Points.Count - 1) < 20) break;

                            var angle = Smoothing.GetAngle(Points[start], Points[i]);

                            for (var j = 0; j <= length; j++)
                            {
                                var newPoint = new PointF
                                {
                                    X = j,
                                    Y = -1 * (float)(Math.Sin(10 * Math.PI * (bottom ? j + 20 : j) / 200) * 10)
                                };
                                cpoints.Add(newPoint);
                            }
                            g.TranslateTransform(Points[start].X, Points[start].Y);
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
                    if (Points.Count == 2)
                    {
                        var lineLength = Smoothing.GetLineLength(Points[0], Points[1]);
                        var cpoints = new List<PointF>();
                        var angle = (float)(Math.Atan2(Points[1].Y - Points[0].Y, Points[1].X - Points[0].X) * 180 / Math.PI);
                        g.TranslateTransform(Points[0].X, Points[0].Y);
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
                            if (point.Y is > -4 and < 4)
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
                        for (var i = 0; i < Points.Count; i++)
                        {
                            var length = Smoothing.GetLineLength(Points[start], Points[i]);
                            switch (length)
                            {
                                case < 20:
                                    continue;
                                case > 20:
                                {
                                    var eh = Points[start];
                                    var meh = Points[i];
                                    var angle2 = (Math.PI / 2) - Math.Atan2(meh.Y - eh.Y, meh.X - eh.X);
                                    var x = (int)Math.Round(Math.Sin(angle2) * 20, MidpointRounding.AwayFromZero);
                                    var y = (int)Math.Round(Math.Cos(angle2) * 20, MidpointRounding.AwayFromZero);
                                    var bleh = new Point(eh.X + x, eh.Y + y);
                                    length = Smoothing.GetLineLength(eh, bleh);
                                    Points.Insert(i, bleh);
                                    break;
                                }
                            }

                            if (GetAggregateLength(i, Points.Count - 1) < 20)
                                break;

                            var angle = (float)(Math.Atan2(Points[i].Y - Points[start].Y, Points[i].X - Points[start].X) * 180 / Math.PI);
                            g.TranslateTransform(Points[start].X, Points[start].Y);
                            g.RotateTransform(angle);
                            for (var j = 0; j < length; j++)
                            {
                                var newPoint = new PointF
                                {
                                    X = j,
                                    Y = -1 * (float)(Math.Sin(10 * Math.PI * (bottom ? j + 20 : j) / 200) * 10)
                                };
                                cpoints.Add(newPoint);
                                if (newPoint.Y is > -4 and < 4)
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
            return true;
        }
    }
}