using System.Drawing;

namespace CoachDraw.Drawables.Items
{
    public sealed class Puck : Item
    {
        private const int Size = 4;
        public override TypeEnum Type => TypeEnum.Puck;
        public override Rectangle? GetHitBox(Point location)
            => new Rectangle(location.X - Size, location.Y - Size, Size * 2, Size * 2);

        public override void Draw(Point location, Color color, Graphics g)
        {
            using var brush = new SolidBrush(color);
            g.FillRectangle(brush, location.X - Size, location.Y - Size / 2, Size * 2, Size);
        }

        public Puck() : base(null, null) { }
    }
}