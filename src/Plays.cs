using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace CoachDraw
{
    public class Play
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public uint Version { get; set; } = Plays.CurrentPlyxVersion;
        public List<DrawObj> Objects { get; set; } = new List<DrawObj>();
        public RinkType RinkType { get; set; } = RinkType.IIHF;
    }

    public static class Plays
    {
        public const int CurrentPlyxVersion = 2;

        // Returns the file version and advances the stream to the data portion.
        public static uint ValidatePLYXHeader(Stream file)
        {
            // Smallest possible PLYX is 17 bytes
            if (file == null || file.Length < 17) return 0;
            file.Position = 0;
            byte[] buf = new byte[4];
            if (file.Read(buf, 0, 4) != 4) throw new IOException("Failed to read header values from plyx file.");
            if (buf[0] != 80 && buf[1] != 76 && buf[2] != 89 && buf[3] != 88) return 0; // Check for "PLYX"
            if (file.Read(buf, 0, 4) != 4) throw new IOException("Failed to read version values from plyx file.");
            return BitConverter.ToUInt32(buf, 0);
        }

        public static Play LoadPLYXFile(string filePath)
        {
            Play result = new Play();
            using (BinaryReader bw = new BinaryReader(File.OpenRead(filePath), Encoding.UTF8))
            {
                result.Version = ValidatePLYXHeader(bw.BaseStream);
                if (result.Version < 1 || result.Version > CurrentPlyxVersion)
                    return null;
                result.Name = bw.ReadString();
                result.Description = bw.ReadString();
                if (result.Version != 1) // Default was IIHF in version 1
                    result.RinkType = (RinkType) bw.ReadByte();
                int numObjs = bw.ReadInt32();
                for (int i = 0; i < numObjs; i++)
                {
                    DrawObj newObj = new DrawObj
                    {
                        objType = (ItemType) bw.ReadByte(),
                        color = ColorTranslator.FromWin32(bw.ReadInt32()),
                        objLabel = bw.ReadInt32(),
                        objLoc =
                        {
                            X = bw.ReadInt32(),
                            Y = bw.ReadInt32()
                        }
                    };
                    if (bw.ReadBoolean())
                    {
                        newObj.objLine = new Line
                        {
                            color = ColorTranslator.FromWin32(bw.ReadInt32()),
                            lineType = (LineType) bw.ReadByte(),
                            endType = (EndType) bw.ReadByte(),
                            lineWidth = bw.ReadByte()
                        };
                        int numPoints = bw.ReadInt32();
                        for (int j = 0; j < numPoints; j++)
                        {
                            Point newPoint = new Point
                            {
                                X = bw.ReadInt32(),
                                Y = bw.ReadInt32()
                            };
                            newObj.objLine.points.Add(newPoint);
                        }
                        newObj.objLine.smoothed = bw.ReadBoolean();
                    }
                    result.Objects.Add(newObj);
                }
                return result;
            }
        }

        public static bool savePLYXFile(string filePath, Play currentPlay, string playName, string playDesc)
        {
            using (var bw = new BinaryWriter(File.Open(filePath, FileMode.Create), Encoding.UTF8))
            {
                bw.Write(new [] { 'P', 'L', 'Y', 'X' });
                bw.Write(currentPlay.Version);
                bw.Write(playName);
                bw.Write(playDesc);
                bw.Write((byte)currentPlay.RinkType);
                bw.Write(currentPlay.Objects.Count);
                foreach (var obj in currentPlay.Objects)
                {
                    bw.Write((byte)obj.objType);
                    bw.Write(ColorTranslator.ToWin32(obj.color));
                    bw.Write(obj.objLabel);
                    bw.Write(obj.objLoc.X);
                    bw.Write(obj.objLoc.Y);
                    bw.Write(obj.objLine != null);
                    if (obj.objLine != null)
                    {
                        bw.Write(ColorTranslator.ToWin32(obj.objLine.color));
                        bw.Write((byte)obj.objLine.lineType);
                        bw.Write((byte)obj.objLine.endType);
                        bw.Write(obj.objLine.lineWidth);
                        bw.Write(obj.objLine.points.Count);
                        foreach (var point in obj.objLine.points)
                        {
                            bw.Write(point.X);
                            bw.Write(point.Y);
                        }
                        bw.Write(obj.objLine.smoothed);
                    }
                }
                bw.Write(new [] { 'E', 'N', 'D' });
            }
            return true;
        }

        public static string GetPLYXName(string filePath)
        {
            if (!File.Exists(filePath)) return "";
            using (BinaryReader br = new BinaryReader(File.OpenRead(filePath), Encoding.UTF8))
            {
                return ValidatePLYXHeader(br.BaseStream) != 1
                    ? "**Invalid play file**"
                    : br.ReadString();
            }
        }

        public static bool RenamePLYX(string filePath, string newName)
        {
            if (!File.Exists(filePath)) return false;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                BinaryReader br = new BinaryReader(fs, Encoding.UTF8);
                if (ValidatePLYXHeader(br.BaseStream) != 1) return false;
                br.ReadString();
                var actualLen = (int)(br.BaseStream.Length - br.BaseStream.Position);
                var bytes = br.ReadBytes(actualLen);
                BinaryWriter bw = new BinaryWriter(fs, Encoding.UTF8);
                bw.BaseStream.Position = 8;
                bw.Write(newName);
                fs.SetLength(bw.BaseStream.Position + actualLen);
                bw.Write(bytes);
            }
            return true;
        }

        public static string GetPLYName(string filePath)
        {
            if (!File.Exists(filePath)) return "";
            using (StreamReader file = new StreamReader(filePath))
            {
                file.ReadLine();
                file.ReadLine();
                return file.ReadLine();
            }
        }

        // TODO: Review
        public static Play LoadPLYFile(string filePath)
        {
            Play result = new Play();
            if (!File.Exists(filePath)) return null;
            try
            {
                using (StreamReader file = new StreamReader(filePath))
                {
                    file.ReadLine(); // Play v01
                    file.ReadLine(); // H00 or H01
                    result.Name = file.ReadLine();
                    int items = int.Parse(file.ReadLine()); //# of items
                    int prevType = 3;
                    for (int i = 1; i <= items; i++)
                    {
                        int type = int.Parse(file.ReadLine());
                        string[] words = file.ReadLine().Split(' ');
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

                            DrawObj newObj = new DrawObj
                            {
                                objLine = new Line
                                {
                                    lineWidth = byte.Parse(words[1]),
                                    lineType = (LineType) byte.Parse(words[2])
                                }
                            };
                            if (int.Parse(words[4]) > 4) words[4] = "0";
                            if (words[4] == "-1") // end type can't be -1, broken object
                            {
                                file.ReadLine();
                                continue;
                            }
                            newObj.objLine.endType = (EndType)byte.Parse(words[4]);
                            newObj.objLine.color = ColorTranslator.FromWin32(int.Parse(words[0]));
                            words = file.ReadLine().Split(' ');
                            // Read line and check points for bad lines (some plays seem to have artifact objects in them)
                            bool valid = false;
                            for (int j = 0; j < words.Length - 1; j += 2)
                            {
                                if (!valid && words[j] != "0" && words[j + 1] != "0") valid = true;
                                newObj.objLine.points.Add(new Point(int.Parse(words[j]), int.Parse(words[j + 1])));
                            }
                            // Remove bad lines
                            if (!valid || newObj.objLine.points.Count == 2 && Smoothing.GetLineLength(newObj.objLine.points[0], newObj.objLine.points[1]) < 20)
                                newObj.objLine = null;
                            result.Objects.Add(newObj);
                        }
                        else if (type == 3)
                        {
                            /* 16711680 -1 1 9 855 290
                             * color, label (-1 for none), number of points (always 1), type, X, Y
                             */
                            var newObj = prevType == 3 ? new DrawObj() : result.Objects[result.Objects.Count - 1];
                            newObj.objType = (ItemType)byte.Parse(words[3]);
                            newObj.objLoc = new Point(int.Parse(words[4]), int.Parse(words[5]));
                            newObj.color = ColorTranslator.FromWin32(int.Parse(words[0]));
                            if (!words[1].Equals("-1")) newObj.objLabel = int.Parse(words[1]);
                            else if (newObj.objType == ItemType.PlayerNumber) newObj.objLabel = 0;
                            if (prevType == 3) result.Objects.Add(newObj);
                        }
                        prevType = type;

                    }

                    // Search and destroy broken, pointless objects (no line, no type, no label, wouldn't really display anything)
                    result.Objects.RemoveAll(o => o.objLine == null && o.objType == ItemType.None && o.objLabel == -1);

                    file.ReadLine(); // Blank line
                    int skipLines = int.Parse(file.ReadLine()); // Skip "attach" list as it is already guaranteed that legit lines will always have an object attached
                    for (int i = 1; i <= skipLines; i++) file.ReadLine();

                    skipLines = int.Parse(file.ReadLine());
                    StringBuilder comment = new StringBuilder();
                    for (int i = 0; i < skipLines; i++)
                    {
                        comment.Append(file.ReadLine().Replace("\"", "") + "\r\n");
                    }
                    result.Description = comment.ToString();
                }
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
