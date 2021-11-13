using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using CoachDraw.Drawables;
using CoachDraw.Drawables.Items;
using CoachDraw.Drawables.Lines;
using CoachDraw.Rink;

namespace CoachDraw
{
    public class Play
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public uint Version { get; set; } = Plays.CurrentPlyxVersion;
        public List<Drawable> Objects { get; set; } = new();
        public RinkType RinkType { get; set; } = RinkType.IIHF;
    }

    public static class Plays
    {
        public const int CurrentPlyxVersion = 2;

        // Returns the file version and advances the stream to the data portion.
        private static bool ValidatePlyxHeader(Stream file, out uint version)
        {
            version = 0;

            // Smallest possible PLYX is 17 bytes
            if (file == null || file.Length < 17)
                return false;
            file.Position = 0;
            var buf = new byte[4];
            if (file.Read(buf, 0, 4) != 4)
                throw new IOException("Failed to read header values from plyx file.");

            // Check for "PLYX"
            if (buf[0] != 80 && buf[1] != 76 && buf[2] != 89 && buf[3] != 88)
                return false;

            if (file.Read(buf, 0, 4) != 4)
                throw new IOException("Failed to read version values from plyx file.");

            version = BitConverter.ToUInt32(buf, 0);
            return version is >= 1 and <= CurrentPlyxVersion;
        }

        public static Play LoadPlyxFile(string filePath)
        {
            var result = new Play();
            using var bw = new BinaryReader(File.OpenRead(filePath), Encoding.UTF8);
            if (!ValidatePlyxHeader(bw.BaseStream, out var version))
                return null;
            result.Version = version;
            result.Name = bw.ReadString();
            result.Description = bw.ReadString();
            if (result.Version != 1) // Default was IIHF in version 1
                result.RinkType = (RinkType) bw.ReadByte();
            var numObjs = bw.ReadInt32();
            for (var i = 0; i < numObjs; i++)
            {
                var type = (Item.TypeEnum)bw.ReadByte();
                var color = ColorTranslator.FromWin32(bw.ReadInt32());
                var playerNumber = bw.ReadInt32();
                var location = new Point(bw.ReadInt32(), bw.ReadInt32());
                Line line = null;
                if (bw.ReadBoolean())
                {
                    line = new Line
                    {
                        Color = ColorTranslator.FromWin32(bw.ReadInt32()),
                        LineType = (LineType) bw.ReadByte(),
                        EndType = (EndType) bw.ReadByte(),
                        LineWidth = bw.ReadByte()
                    };
                    var numPoints = bw.ReadInt32();
                    for (var j = 0; j < numPoints; j++)
                    {
                        var newPoint = new Point
                        {
                            X = bw.ReadInt32(),
                            Y = bw.ReadInt32()
                        };
                        line.Points.Add(newPoint);
                    }
                    line.Smoothed = bw.ReadBoolean();
                }
                var item = ItemBuilder.Build(type, playerNumber != -1 ? playerNumber : null);
                var newObj = new Drawable(location, item)
                {
                    Line = line,
                    Color = color
                };
                result.Objects.Add(newObj);
            }
            return result;
        }

        public static bool SavePlyxFile(string filePath, Play currentPlay, string playName, string playDesc)
        {
            using var bw = new BinaryWriter(File.Open(filePath, FileMode.Create), Encoding.UTF8);
            bw.Write(new [] { 'P', 'L', 'Y', 'X' });
            bw.Write(currentPlay.Version);
            bw.Write(playName);
            bw.Write(playDesc);
            bw.Write((byte)currentPlay.RinkType);
            bw.Write(currentPlay.Objects.Count);
            foreach (var obj in currentPlay.Objects)
            {
                bw.Write((byte)obj.Item.Type);
                bw.Write(ColorTranslator.ToWin32(obj.Color));
                bw.Write(obj.Item.Number ?? -1);
                bw.Write(obj.Location.X);
                bw.Write(obj.Location.Y);
                bw.Write(obj.Line != null);
                if (obj.Line != null)
                {
                    bw.Write(ColorTranslator.ToWin32(obj.Line.Color));
                    bw.Write((byte)obj.Line.LineType);
                    bw.Write((byte)obj.Line.EndType);
                    bw.Write(obj.Line.LineWidth);
                    bw.Write(obj.Line.Points.Count);
                    foreach (var point in obj.Line.Points)
                    {
                        bw.Write(point.X);
                        bw.Write(point.Y);
                    }
                    bw.Write(obj.Line.Smoothed);
                }
            }
            bw.Write(new [] { 'E', 'N', 'D' });
            return true;
        }

        public static string GetPlyxName(string filePath)
        {
            if (!File.Exists(filePath))
                return "";
            using var br = new BinaryReader(File.OpenRead(filePath), Encoding.UTF8);
            return ValidatePlyxHeader(br.BaseStream, out _)
                ? br.ReadString()
                : "**Invalid play file**";
        }

        public static bool RenamePlyx(string filePath, string newName)
        {
            if (!File.Exists(filePath))
                return false;
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
            var br = new BinaryReader(fs, Encoding.UTF8);
            if (!ValidatePlyxHeader(br.BaseStream, out _))
                return false;
            br.ReadString();
            var actualLen = (int)(br.BaseStream.Length - br.BaseStream.Position);
            var bytes = br.ReadBytes(actualLen);
            var bw = new BinaryWriter(fs, Encoding.UTF8);
            bw.BaseStream.Position = 8;
            bw.Write(newName);
            fs.SetLength(bw.BaseStream.Position + actualLen);
            bw.Write(bytes);
            return true;
        }

        public static string GetPlyName(string filePath)
        {
            if (!File.Exists(filePath))
                return "";
            using var file = new StreamReader(filePath);
            file.ReadLine();
            file.ReadLine();
            return file.ReadLine();
        }

        // TODO: Review
        public static Play LoadPlyFile(string filePath)
        {
            var result = new Play();
            if (!File.Exists(filePath)) return null;
            try
            {
                using var file = new StreamReader(filePath);
                file.ReadLine(); // Play v01
                file.ReadLine(); // H00 or H01
                result.Name = file.ReadLine();
                var items = int.Parse(file.ReadLine()); //# of items
                Line line = null;
                for (var i = 1; i <= items; i++)
                {
                    var type = int.Parse(file.ReadLine());
                    var words = file.ReadLine().Split(' ');
                    // type 4 = line, type 3 = object
                    // lines always have an object attached
                    if (type == 4)
                    {
                        /* 16711680 2 0 126 1
                             * color, line thickness, line type, number of points, end type
                             * if only one point, this is a blank line before an object, so skip it
                             */
                        if (words[3] == "1")
                        {
                            file.ReadLine();
                            continue;
                        }

                        line = new Line
                        {
                            LineWidth = byte.Parse(words[1]),
                            LineType = (LineType)byte.Parse(words[2])
                        };
                        if (int.Parse(words[4]) > 4)
                            words[4] = "0";
                        if (words[4] == "-1") // end type can't be -1, broken object
                        {
                            file.ReadLine();
                            continue;
                        }
                        line.EndType = (EndType)byte.Parse(words[4]);
                        line.Color = ColorTranslator.FromWin32(int.Parse(words[0]));
                        words = file.ReadLine().Split(' ');
                        // Read line and check points for bad lines (some plays seem to have artifact objects in them)
                        var valid = false;
                        for (var j = 0; j < words.Length - 1; j += 2)
                        {
                            if (!valid && words[j] != "0" && words[j + 1] != "0") valid = true;
                            line.Points.Add(new Point(int.Parse(words[j]), int.Parse(words[j + 1])));
                        }
                        // Remove bad lines
                        if (!valid || line.Points.Count == 2 && Smoothing.GetLineLength(line.Points[0], line.Points[1]) < 20)
                            line = null;
                    }
                    else if (type == 3)
                    {
                        /* 16711680 -1 1 9 855 290
                             * color, label (-1 for none), number of points (always 1), type, X, Y
                             */
                        var itemType = (Item.TypeEnum)byte.Parse(words[3]);
                        var location = new Point(int.Parse(words[4]), int.Parse(words[5]));
                        var color = ColorTranslator.FromWin32(int.Parse(words[0]));
                        int? playerNumber = null;
                        if (!words[1].Equals("-1"))
                            playerNumber = int.Parse(words[1]);
                        // -1 is an invalid player number, we'll just make the type none
                        else if (itemType == Item.TypeEnum.PlayerNumber)
                            itemType = Item.TypeEnum.None;

                        var newObj = new Drawable(location, ItemBuilder.Build(itemType, playerNumber))
                        {
                            Color = color,
                            Line = line
                        };

                        line = null;

                        result.Objects.Add(newObj);
                    }
                }

                // Search and destroy broken, pointless objects (no line, no type, no label, wouldn't really display anything)
                result.Objects.RemoveAll(o => o.Line == null && o.Item.Type == Item.TypeEnum.None && o.Item.Number is -1 or null);

                file.ReadLine(); // Blank line
                var skipLines = int.Parse(file.ReadLine()); // Skip "attach" list as it is already guaranteed that legit lines will always have an object attached
                for (var i = 1; i <= skipLines; i++)
                    file.ReadLine();

                skipLines = int.Parse(file.ReadLine());
                var comment = new StringBuilder();
                for (var i = 0; i < skipLines; i++)
                {
                    comment.Append(file.ReadLine().Replace("\"", "") + "\r\n");
                }
                result.Description = comment.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: {e}");
                return null;
            }
            return result;
        }
    }
}
