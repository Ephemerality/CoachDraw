using System.Diagnostics;
using System.Drawing;
using CoachDraw.Extensions;

namespace CoachDraw.Model
{
    public sealed class DrawObj
    {
        public ItemType ObjType = ItemType.None;
        public Line ObjLine;
        public int ObjLabel = -1;
        public Point ObjLoc;
        public Color Color = Color.Blue;
        public Region HitBox;

        private void SetHitBox(Rectangle box)
        {
            HitBox = new Region(box);
        }

        public void Draw(Graphics g, bool selected)
        {
            var myPen = selected ? new Pen(Color.Invert(), 1) : new Pen(Color, 1);
            var drawString = "";
            var fontSize = 12;
            var lineSkip = new Rectangle();
            switch (ObjType)
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
                    g.FillPolygon(new SolidBrush(Color), new[]
                    {
                        new Point(ObjLoc.X - 6, ObjLoc.Y + 6),
                        new Point(ObjLoc.X + 6, ObjLoc.Y + 6),
                        new Point(ObjLoc.X, ObjLoc.Y - 6)
                    });
                    SetHitBox(new Rectangle(ObjLoc.X - 6, ObjLoc.Y - 6, 12, 12));
                    break;
                case ItemType.Puck:
                    g.FillRectangle(new SolidBrush(Color), ObjLoc.X - 4, ObjLoc.Y - 2, 8, 4);
                    SetHitBox(new Rectangle(ObjLoc.X - 4, ObjLoc.Y - 2, 8, 4));
                    break;
                case ItemType.Pucks:
                    g.FillEllipse(new SolidBrush(Color), ObjLoc.X - 2, ObjLoc.Y - 2, 4, 4);
                    g.FillEllipse(new SolidBrush(Color), ObjLoc.X - 6, ObjLoc.Y - 6, 4, 4);
                    g.FillEllipse(new SolidBrush(Color), ObjLoc.X + 6, ObjLoc.Y + 6, 4, 4);
                    g.FillEllipse(new SolidBrush(Color), ObjLoc.X - 6, ObjLoc.Y + 6, 4, 4);
                    g.FillEllipse(new SolidBrush(Color), ObjLoc.X + 6, ObjLoc.Y - 6, 4, 4);
                    SetHitBox(new Rectangle(ObjLoc.X - 6, ObjLoc.Y - 6, 12, 12));
                    break;
                case ItemType.None:
                case ItemType.PlayerNumber:
                    break;
                default:
                    Debugger.Break();
                    break;
            }
            if (ObjLabel >= 0) drawString += ObjLabel;
            if (drawString != "")
            {
                var fSize = g.MeasureString(drawString, new Font("MS Sans Serif", fontSize));
                var x = ObjLoc.X - (int)(fSize.Width / 2);
                var y = ObjLoc.Y - (int)(fSize.Height / 2);
                lineSkip = new Rectangle(x, y, (int)fSize.Width, (int)fSize.Height);
                SetHitBox(lineSkip);
                g.DrawString(drawString, new Font("MS Sans Serif", fontSize), new SolidBrush(myPen.Color), x, y);
            }
            if (ObjLine != null && !ObjLine.Draw(g, selected, lineSkip))
                ObjLine = null; // line was actually invalid, kill it
        }
    }
}
