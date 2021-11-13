using System;
using System.Drawing;
using CoachDraw.Drawables.Items;
using CoachDraw.Drawables.Lines;
using CoachDraw.Extensions;
using JetBrains.Annotations;

namespace CoachDraw.Drawables
{
    public sealed class Drawable
    {
        public Drawable(Point location, [NotNull] Item item)
        {
            Item = item;
            Location = location;
        }

        public Point Location { get; }

        [NotNull]
        public Item Item
        {
            get => _item;
            set => _item = value ?? throw new InvalidOperationException($"{nameof(Item)} must not be null");
        }

        private Item _item;

        [CanBeNull]
        public Line Line { get; set; }

        public Color Color { get; set; } = Color.Blue;

        [CanBeNull]
        public Region HitBox { get; set; }

        public void Draw(Graphics g, bool selected)
        {
            using var myPen = selected
                ? new Pen(Color.Invert(), 1)
                : new Pen(Color, 1);

            Item.Draw(Location, Color, g);
            var hitbox = Item.GetHitBox(Location);
            var text = $"{Item.Symbol ?? ""}{Item.Number?.ToString() ?? ""}";
            if (!string.IsNullOrWhiteSpace(text))
            {
                var fSize = g.MeasureString(text, new Font(Item.FontName, Item.FontSize));
                var x = Location.X - (int)(fSize.Width / 2);
                var y = Location.Y - (int)(fSize.Height / 2);
                hitbox = new Rectangle(x, y, (int)fSize.Width, (int)fSize.Height);
                g.DrawString(text, new Font(Item.FontName, Item.FontSize), new SolidBrush(myPen.Color), x, y);
            }

            if (hitbox.HasValue)
                HitBox = new Region(hitbox.Value);

            // if a line is present, draw it, but remove it if it was deemed invalid by the drawing engine
            if (Line != null && !Line.Draw(g, selected, hitbox ?? Rectangle.Empty))
                Line = null;
        }
    }
}