using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace CoachDraw
{
    public class PlayInfo
    {
        public string Name;
        public string Desc;
    }

    public static class Plays
    {
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

        public static PlayInfo LoadPLYXFile(string filePath, ref List<drawObj> objs)
        {
            PlayInfo result = new PlayInfo();
            using (BinaryReader bw = new BinaryReader(File.OpenRead(filePath), Encoding.UTF8))
            {
                if (ValidatePLYXHeader(bw.BaseStream) != 1) return null;
                result.Name = bw.ReadString();
                result.Desc = bw.ReadString();
                int numObjs = bw.ReadInt32();
                for (int i = 0; i < numObjs; i++)
                {
                    drawObj newObj = new drawObj();
                    newObj.objType = (ItemType)bw.ReadByte();
                    newObj.color = ColorTranslator.FromWin32(bw.ReadInt32());
                    newObj.objLabel = bw.ReadInt32();
                    newObj.objLoc.X = bw.ReadInt32();
                    newObj.objLoc.Y = bw.ReadInt32();
                    if (bw.ReadBoolean())
                    {
                        newObj.objLine = new Line();
                        newObj.objLine.color = ColorTranslator.FromWin32(bw.ReadInt32());
                        newObj.objLine.lineType = (LineType)bw.ReadByte();
                        newObj.objLine.endType = (EndType)bw.ReadByte();
                        newObj.objLine.lineWidth = bw.ReadByte();
                        int numPoints = bw.ReadInt32();
                        for (int j = 0; j < numPoints; j++)
                        {
                            Point newPoint = new Point();
                            newPoint.X = bw.ReadInt32();
                            newPoint.Y = bw.ReadInt32();
                            newObj.objLine.points.Add(newPoint);
                        }
                        newObj.objLine.smoothed = bw.ReadBoolean();
                    }
                    objs.Add(newObj);
                }
                return result;
            }
        }

        public static bool savePLYXFile(string filePath, List<drawObj> objs, string playName, string playDesc, uint plyxVersion)
        {
            using (BinaryWriter bw = new BinaryWriter(File.Open(filePath, FileMode.Create), Encoding.UTF8))
            {
                bw.Write(new char[] { 'P', 'L', 'Y', 'X' });
                bw.Write(plyxVersion);
                bw.Write(playName);
                bw.Write(playDesc);
                bw.Write(objs.Count);
                for (int i = 0; i < objs.Count; i++)
                {
                    bw.Write((byte)objs[i].objType);
                    bw.Write(ColorTranslator.ToWin32(objs[i].color));
                    bw.Write(objs[i].objLabel);
                    bw.Write(objs[i].objLoc.X);
                    bw.Write(objs[i].objLoc.Y);
                    bw.Write((objs[i].objLine != null ? true : false));
                    if (objs[i].objLine != null)
                    {
                        bw.Write(ColorTranslator.ToWin32(objs[i].objLine.color));
                        bw.Write((byte)objs[i].objLine.lineType);
                        bw.Write((byte)objs[i].objLine.endType);
                        bw.Write(objs[i].objLine.lineWidth);
                        bw.Write(objs[i].objLine.points.Count);
                        for (int j = 0; j < objs[i].objLine.points.Count; j++)
                        {
                            bw.Write(objs[i].objLine.points[j].X);
                            bw.Write(objs[i].objLine.points[j].Y);
                        }
                        bw.Write(objs[i].objLine.smoothed);
                    }
                }
                bw.Write(new char[] { 'E', 'N', 'D' });
            }
            return true;
        }

        public static string GetPLYXName(string filePath)
        {
            if (!File.Exists(filePath)) return "";
            using (BinaryReader br = new BinaryReader(File.OpenRead(filePath), Encoding.UTF8))
            {
                if (ValidatePLYXHeader(br.BaseStream) != 1) return "**Invalid play file**";
                return br.ReadString();
            }
        }

        public static bool RenamePLYX(string filePath, string newName)
        {
            if (!File.Exists(filePath)) return false;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                byte[] bytes;
                int actualLen = 0;
                BinaryReader br = new BinaryReader(fs, Encoding.UTF8);
                if (ValidatePLYXHeader(br.BaseStream) != 1) return false;
                br.ReadString();
                actualLen = (int)(br.BaseStream.Length - br.BaseStream.Position);
                bytes = br.ReadBytes(actualLen);
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

        public static PlayInfo LoadPLYFile(string filePath, ref List<drawObj> objs)
        {
            PlayInfo result = new PlayInfo();
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
                            drawObj newObj = new drawObj();
                            newObj.objLine = new Line();
                            newObj.objLine.lineWidth = byte.Parse(words[1]);
                            newObj.objLine.lineType = (LineType)byte.Parse(words[2]);
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
                            if (!valid || (newObj.objLine.points.Count == 2 && Smoothing.getLineLength(newObj.objLine.points[0], newObj.objLine.points[1]) < 20))
                                newObj.objLine = null;
                            objs.Add(newObj);
                        }
                        else if (type == 3)
                        {
                            /* 16711680 -1 1 9 855 290
                             * color, label (-1 for none), number of points (always 1), type, X, Y
                             */
                            drawObj newObj;
                            if (prevType == 3) newObj = new drawObj();
                            else newObj = objs[objs.Count - 1];
                            newObj.objType = (ItemType)byte.Parse(words[3]);
                            newObj.objLoc = new Point(int.Parse(words[4]), int.Parse(words[5]));
                            newObj.color = ColorTranslator.FromWin32(int.Parse(words[0]));
                            if (!words[1].Equals("-1")) newObj.objLabel = int.Parse(words[1]);
                            else if (newObj.objType == ItemType.PlayerNumber) newObj.objLabel = 0;
                            if (prevType == 3) objs.Add(newObj);
                        }
                        prevType = type;

                    }

                    // Search and destroy broken, pointless objects (no line, no type, no label, wouldn't really display anything)
                    objs.RemoveAll(o => o.objLine == null && o.objType == ItemType.None && o.objLabel == -1);

                    file.ReadLine(); // Blank line
                    int skipLines = int.Parse(file.ReadLine()); // Skip "attach" list as it is already guaranteed that legit lines will always have an object attached
                    for (int i = 1; i <= skipLines; i++) file.ReadLine();

                    skipLines = int.Parse(file.ReadLine());
                    StringBuilder comment = new StringBuilder();
                    for (int i = 0; i < skipLines; i++)
                    {
                        comment.Append(file.ReadLine().Replace("\"", "") + "\r\n");
                    }
                    result.Desc = comment.ToString();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e);
                Debugger.Break();
                return null;
            }
            return result;
        }
    }
}
