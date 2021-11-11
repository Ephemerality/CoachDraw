using System.IO;
using System.Linq;

namespace CoachDraw.Extensions
{
    internal static class StringExtensions
    {
        public static string StripInvalid(this string input)
        {
            return Path.GetInvalidFileNameChars().Aggregate(input, (current, c) => current.Replace(c, '-'));
        }
    }
}
