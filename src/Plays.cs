using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using CoachDraw.Model;
using CoachDraw.Rink;

namespace CoachDraw
{
    public class Play
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public uint Version { get; set; } = Plays.CurrentPlyxVersion;
        public List<DrawObj> Objects { get; set; } = new();
        public RinkType RinkType { get; set; } = RinkType.IIHF;
    }

    public static class Plays
    {
        public const int CurrentPlyxVersion = 2;

        // Returns the file version and advances the stream to the data portion.
        public static uint ValidatePlyxHeader(Stream file)
        {
            // Smallest possible PLYX is 17 bytes
            if (file == null || file.Length < 17)
                return 0;
            file.Position = 0;
            var buf = new byte[4];
            if (file.Read(buf, 0, 4) != 4)
                throw new IOException("Failed to read header values from plyx file.");

            // Check for "PLYX"
            if (buf[0] != 80 && buf[1] != 76 && buf[2] != 89 && buf[3] != 88)
                return 0;

            if (file.Read(buf, 0, 4) != 4)
                throw new IOException("Failed to read version values from plyx file.");

            return BitConverter.ToUInt32(buf, 0);
        }

        public static Play LoadPlyxFile(string filePath)
        {
            var result = new Play();
            using var bw = new BinaryReader(File.OpenRead(filePath), Encoding.UTF8);
            result.Version = ValidatePlyxHeader(bw.BaseStream);
            if (result.Version < 1 || result.Version > CurrentPlyxVersion)
                return null;
            result.Name = bw.ReadString();
            result.Description = bw.ReadString();
            if (result.Version != 1) // Default was IIHF in version 1
                result.RinkType = (RinkType) bw.ReadByte();
            var numObjs = bw.ReadInt32();
            for (var i = 0; i < numObjs; i++)
            {
                var newObj = new DrawObj
                {
                    ObjType = (ItemType) bw.ReadByte(),
                    Color = ColorTranslator.FromWin32(bw.ReadInt32()),
                    ObjLabel = bw.ReadInt32(),
                    ObjLoc =
                    {
                        X = bw.ReadInt32(),
                        Y = bw.ReadInt32()
                    }
                };
                if (bw.ReadBoolean())
                {
                    newObj.ObjLine = new Line
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
                        newObj.ObjLine.Points.Add(newPoint);
                    }
                    newObj.ObjLine.Smoothed = bw.ReadBoolean();
                }
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
                bw.Write((byte)obj.ObjType);
                bw.Write(ColorTranslator.ToWin32(obj.Color));
                bw.Write(obj.ObjLabel);
                bw.Write(obj.ObjLoc.X);
                bw.Write(obj.ObjLoc.Y);
                bw.Write(obj.ObjLine != null);
                if (obj.ObjLine != null)
                {
                    bw.Write(ColorTranslator.ToWin32(obj.ObjLine.Color));
                    bw.Write((byte)obj.ObjLine.LineType);
                    bw.Write((byte)obj.ObjLine.EndType);
                    bw.Write(obj.ObjLine.LineWidth);
                    bw.Write(obj.ObjLine.Points.Count);
                    foreach (var point in obj.ObjLine.Points)
                    {
                        bw.Write(point.X);
                        bw.Write(point.Y);
                    }
                    bw.Write(obj.ObjLine.Smoothed);
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
            return ValidatePlyxHeader(br.BaseStream) != 1
                ? "**Invalid play file**"
                : br.ReadString();
        }

        public static bool RenamePlyx(string filePath, string newName)
        {
            if (!File.Exists(filePath))
                return false;
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
            var br = new BinaryReader(fs, Encoding.UTF8);
            if (ValidatePlyxHeader(br.BaseStream) != 1)
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
                var prevType = 3;
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
                            prevType = 3;
                            file.ReadLine();
                            continue;
                        }

                        var newObj = new DrawObj
                        {
                            ObjLine = new Line
                            {
                                LineWidth = byte.Parse(words[1]),
                                LineType = (LineType) byte.Parse(words[2])
                            }
                        };
                        if (int.Parse(words[4]) > 4) words[4] = "0";
                        if (words[4] == "-1") // end type can't be -1, broken object
                        {
                            file.ReadLine();
                            continue;
                        }
                        newObj.ObjLine.EndType = (EndType)byte.Parse(words[4]);
                        newObj.ObjLine.Color = ColorTranslator.FromWin32(int.Parse(words[0]));
                        words = file.ReadLine().Split(' ');
                        // Read line and check points for bad lines (some plays seem to have artifact objects in them)
                        var valid = false;
                        for (var j = 0; j < words.Length - 1; j += 2)
                        {
                            if (!valid && words[j] != "0" && words[j + 1] != "0") valid = true;
                            newObj.ObjLine.Points.Add(new Point(int.Parse(words[j]), int.Parse(words[j + 1])));
                        }
                        // Remove bad lines
                        if (!valid || newObj.ObjLine.Points.Count == 2 && Smoothing.GetLineLength(newObj.ObjLine.Points[0], newObj.ObjLine.Points[1]) < 20)
                            newObj.ObjLine = null;
                        result.Objects.Add(newObj);
                    }
                    else if (type == 3)
                    {
                        /* 16711680 -1 1 9 855 290
                             * color, label (-1 for none), number of points (always 1), type, X, Y
                             */
                        var newObj = prevType == 3 ? new DrawObj() : result.Objects[^1];
                        newObj.ObjType = (ItemType)byte.Parse(words[3]);
                        newObj.ObjLoc = new Point(int.Parse(words[4]), int.Parse(words[5]));
                        newObj.Color = ColorTranslator.FromWin32(int.Parse(words[0]));
                        if (!words[1].Equals("-1"))
                            newObj.ObjLabel = int.Parse(words[1]);
                        else if (newObj.ObjType == ItemType.PlayerNumber)
                            newObj.ObjLabel = 0;
                        if (prevType == 3)
                            result.Objects.Add(newObj);
                    }
                    prevType = type;

                }

                // Search and destroy broken, pointless objects (no line, no type, no label, wouldn't really display anything)
                result.Objects.RemoveAll(o => o.ObjLine == null && o.ObjType == ItemType.None && o.ObjLabel == -1);

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
