using System.Drawing;

namespace CoachDraw.Drawables.Items
{
    public sealed class Pucks : Item
    {
        private const int Size = 6;
        public override TypeEnum Type => TypeEnum.Pucks;
        public override Rectangle? GetHitBox(Point location)
            => new Rectangle(location.X - Size, location.Y - Size, Size * 2, Size * 2);

        public override void Draw(Point location, Color color, Graphics g)
        {
            using var brush = new SolidBrush(color);
            g.FillEllipse(brush, location.X - 2, location.Y - 2, 4, 4);
            g.FillEllipse(brush, location.X - Size, location.Y - Size, 4, 4);
            g.FillEllipse(brush, location.X + Size, location.Y + Size, 4, 4);
            g.FillEllipse(brush, location.X - Size, location.Y + Size, 4, 4);
            g.FillEllipse(brush, location.X + Size, location.Y - Size, 4, 4);
        }

        public Pucks() : base(null, null) { }
    }
}