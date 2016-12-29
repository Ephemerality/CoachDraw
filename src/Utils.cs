using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CoachDraw
{
    static class Utils
    {
        public static string StripInvalid(string input)
        {
            return Path.GetInvalidFileNameChars().Aggregate(input, (current, c) => current.Replace(c, '-'));
        }
    }
}
