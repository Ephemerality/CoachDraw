using System.Drawing;
using JetBrains.Annotations;

namespace CoachDraw.Drawables.Items
{
    public abstract class Item
    {
        public enum TypeEnum : byte
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

        public abstract TypeEnum Type { get; }

        [CanBeNull]
        public virtual string Symbol { get; } = null;

        public virtual int? Number { get; } = null;

        public string FontName { get; } = "MS Sans Serif";

        public virtual float FontSize { get; } = 12;

        public virtual void Draw(Point location, Color color, Graphics g) { }

        public virtual Rectangle? GetHitBox(Point location) => null;

        protected Item([CanBeNull] string symbol, int? number)
        {
            Symbol = symbol;
            Number = number;
        }
    }
}