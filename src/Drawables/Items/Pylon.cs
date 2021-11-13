using System.Drawing;

namespace CoachDraw.Drawables.Items
{
    public sealed class Pylon : Item
    {
        private const int Size = 6;
        public override TypeEnum Type => TypeEnum.Pylon;
        public override Rectangle? GetHitBox(Point location)
            => new Rectangle(location.X - Size, location.Y - Size, Size * 2, Size * 2);

        public override void Draw(Point location, Color color, Graphics g)
        {
            using var brush = new SolidBrush(color);
            g.FillPolygon(brush, new[]
            {
                new Point(location.X - Size, location.Y + Size),
                new Point(location.X + Size, location.Y + Size),
                new Point(location.X, location.Y - Size)
            });
        }

        public Pylon() : base(null, null) { }
    }
}