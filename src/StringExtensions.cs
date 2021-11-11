using System.Linq;
using System.IO;

namespace CoachDraw
{
    internal static class StringExtensions
    {
        public static string StripInvalid(this string input)
        {
            return Path.GetInvalidFileNameChars().Aggregate(input, (current, c) => current.Replace(c, '-'));
        }
    }
}
