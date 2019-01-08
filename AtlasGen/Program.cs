using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using LunarLabs.Utils;
using LunarLabs.Parser;
using LunarLabs.Parser.JSON;
using LunarLabs.Parser.CSV;
using LunarLabs.Parser.XML;

namespace CSSAtlasGen
{
    public struct Margin
    {
        public int X;
        public int Y;
    }

    class Program
    {
        static void FixPath(ref string path)
        {
            if (path == null)
            {
                return;
            }

            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        static DataNode ExportToNode(string[] files, Dictionary<string, Bitmap> images, Dictionary<string, Margin> margins, RectanglePacker<string> packer, bool giveNames)
        {
            var node = DataNode.CreateArray("images");
            foreach (var file in files)
            {
                int x, y;
                packer.GetRect(file, out x, out y);

                var margin = margins[file];
                x += margin.X;
                y += margin.Y;

                var img = images[file];

                var child = DataNode.CreateObject(giveNames ? "image": null);
                child.AddField("name", Path.GetFileNameWithoutExtension(file));
                child.AddField("x", x);
                child.AddField("y", y);
                child.AddField("width", img.Width);
                child.AddField("height", img.Height);
                node.AddNode(child);
            }
            return node;
        }

        // example args: -input.path=D:\some\path\to\images\here -filter=*.jpg -prefix=team -output.extension=jpg -output.path= -output.resize=240
        static void Main(string[] args)
        {
            string folder = null;
            string filter = "*.*";
            string outputPath = Directory.GetCurrentDirectory();
            string cssPath = null;
            string jsonPath = null;
            string xmlPath = null;
            string csvPath = null;
            string outputExtension = "jpg";
            string prefix = null;
            int resize = 0;

            var globalMargin = new Margin();
            bool normalize = false;

            foreach (var entry in args)
            {
                var arg = entry;

                if (!arg.StartsWith("-"))
                {
                    Console.WriteLine("Invalid argument: " + arg);
                    return;
                }

                arg = arg.Substring(1);

                var temp = arg.Split(new char[] { '=' }, 2);

                var key = temp[0].ToLower();
                var val = temp.Length == 2 ? temp[1] : null;

                switch (key)
                {
                    case "input.path": folder = val; break;
                    case "input.filter": filter = val; break;
                    case "prefix": prefix = val; break;
                    case "output.extension": outputExtension = val; break;
                    case "output.path": outputPath = val; break;
                    case "output.resize": resize = int.Parse(val); break;
                    case "css.path": cssPath = val; break;
                    case "json.path": jsonPath = val; break;
                    case "xml.path": xmlPath = val; break;
                    case "csv.path": csvPath = val; break;
                    case "margin.X": globalMargin.X = int.Parse(val); break;
                    case "margin.Y": globalMargin.Y = int.Parse(val); break;
                    case "margin": globalMargin.X = int.Parse(val); globalMargin.Y = globalMargin.X; break;
                    case "normalize": normalize = bool.Parse(val); break;
                }
            }

            if (normalize && resize > 0)
            {
                Console.WriteLine("Normalize and resize options are mutually exclusive.");
                return;
            }

            FixPath(ref outputPath);
            FixPath(ref cssPath);
            FixPath(ref jsonPath);
            FixPath(ref xmlPath);
            FixPath(ref csvPath);

            if (folder == null)
            {
                Console.WriteLine("Please specify a folder. Eg: -input.path=some_path");
                return;
            }

            if (prefix == null)
            {
                Console.WriteLine("Please specify a atlas prefix. Eg: --prefix=something");
                return;
            }

            var outPicName = prefix + "." + outputExtension;
            var outCSSName = prefix + ".css";

            var files = Directory.GetFiles(folder, filter);

            var images = new Dictionary<string, Bitmap>();

            int count = 0;

            int avgWidth = 0;
            int avgHeight = 0;

            int maxWidth = 0;
            int maxHeight = 0;

            foreach (var file in files)
            {
                var img = new Bitmap(Bitmap.FromFile(file));

                if (resize != 0)
                {
                    img = new Bitmap(img, new Size(resize, resize));
                }

                images[file] = img;
                count++;

                maxWidth = Math.Max(maxWidth, img.Width);
                maxHeight = Math.Max(maxHeight, img.Height);

                avgWidth += img.Width;
                avgHeight += img.Height;
            }

            int maxSize = Math.Max(maxHeight, maxWidth);

            avgWidth /= count;
            avgHeight /= count;

            int side = (int)Math.Ceiling(Math.Sqrt(count));

            int atlasWidth = avgWidth * side;
            int atlasHeight = avgHeight * side;

            int tot;

            int tries = 0;
            RectanglePacker<string> packer;

            var margins = new Dictionary<string, Margin>();

            if (normalize)
            {
                foreach (var file in files)
                {
                    var img = images[file];
                    var margin = new Margin()
                    {
                        X = (maxSize - img.Width) / 2 + globalMargin.X,
                        Y = (maxSize - img.Height) / 2 + globalMargin.Y,
                    };
                    margins[file] = margin;
                }
            }
            else
            {
                foreach (var file in files)
                {
                    margins[file] = globalMargin;
                }
            }

            do
            {
                packer = new RectanglePacker<string>();


                foreach (var entry in images)
                {
                    var margin = margins[entry.Key];
                    packer.AddRect(entry.Value.Width + margin.X * 2, entry.Value.Height + margin.Y * 2, entry.Key);
                }

                tot = packer.Pack(0, 0, atlasWidth, atlasHeight);
                if (tot == 0)
                {
                    break;
                }

                if (tries % 2 == 0)
                {
                    atlasWidth *= 2;
                }
                else
                {
                    atlasHeight *= 2;
                }

                tries++;
                if (tries > 5)
                {
                    break;
                }
            } while (true);

            if (tot == 0)
            {
                var output = new Bitmap(atlasWidth, atlasHeight);

                using (Graphics g = Graphics.FromImage(output))
                {
                    foreach (var file in files)
                    {
                        int x, y;
                        packer.GetRect(file, out x, out y);

                        var margin = margins[file];
                        x += margin.X;
                        y += margin.Y;

                        var img = images[file];
                        g.DrawImage(img, x, y, img.Width, img.Height);

                        Console.WriteLine("Merged " + file);
                    }
                }

                output.Save(outputPath + outPicName);

                Console.WriteLine("Generated " + outputPath + outPicName);

                if (cssPath != null)
                {
                    var sb = new StringBuilder();

                    int index = 0;
                    foreach (var file in files)
                    {
                        var name = Path.GetFileNameWithoutExtension(file).ToLower();

                        if (index > 0)
                        {
                            sb.Append(", ");
                        }

                        if (index % side == side - 1)
                        {
                            sb.AppendLine();
                        }

                        sb.Append($".{prefix}-{name}");

                        index++;
                    }

                    sb.Append('{');
                    sb.AppendLine();
                    sb.AppendLine($"\tbackground-image: url('{outPicName}');");
                    sb.AppendLine("\tbackground-repeat: no-repeat;");
                    sb.Append('}');
                    sb.AppendLine();

                    foreach (var file in files)
                    {
                        int x, y;
                        packer.GetRect(file, out x, out y);

                        var margin = margins[file];
                        x += margin.X;
                        y += margin.Y;

                        var name = Path.GetFileNameWithoutExtension(file).ToLower();
                        var img = images[file];

                        sb.AppendLine();
                        sb.AppendLine("." + prefix + "-" + name + " {");
                        sb.AppendLine($"\twidth: {img.Width}px;");
                        sb.AppendLine($"\theight: {img.Height}px;");
                        sb.AppendLine($"\tbackground-position: -{x}px -{y}px;");
                        sb.Append('}');
                        sb.AppendLine();
                    }

                    File.WriteAllText(cssPath + outCSSName, sb.ToString());

                    Console.WriteLine("Generated " + cssPath + outCSSName);
                }

                if (xmlPath!= null || csvPath != null || jsonPath != null)
                {

                    if (jsonPath != null)
                    {
                        var node = ExportToNode(files, images, margins, packer, false);
                        var json = JSONWriter.WriteToString(node);
                        File.WriteAllText(jsonPath + prefix + ".json", json);
                    }

                    if (xmlPath != null)
                    {
                        var node = ExportToNode(files, images, margins, packer, true);
                        var xml = XMLWriter.WriteToString(node);
                        File.WriteAllText(xmlPath + prefix + ".xml", xml);
                    }

                    if (csvPath != null)
                    {
                        var node = ExportToNode(files, images, margins, packer, false);
                        var csv = CSVWriter.WriteToString(node);
                        File.WriteAllText(csvPath + prefix + ".csv", csv);
                    }
                }
            }
            else
            {
                Console.WriteLine("Failed packing...");
            }
        }
    }
}
