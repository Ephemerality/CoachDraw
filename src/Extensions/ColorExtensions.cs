using System.Drawing;

namespace CoachDraw.Extensions
{
    public static class ColorExtensions
    {
        public static Color Invert(this Color c)
            => Color.FromArgb(255 - c.R, 255 - c.G, 255 - c.B);
    }
}