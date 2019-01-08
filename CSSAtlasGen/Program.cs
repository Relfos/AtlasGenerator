using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Lunar.Utils;

namespace CSSAtlasGen
{
    class Program
    {
        // example args: -input.path=D:\some\path\to\images\here -filter=*.jpg -prefix=team -output.extension=jpg -output.path= -output.resize=240
        static void Main(string[] args)
        {
            string folder = null;
            string filter = "*.*";
            string outputPath = Directory.GetCurrentDirectory();
            string cssPath = outputPath;
            string outputExtension = "jpg";
            string prefix = null;
            int resize = 0;

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
                    case "filter": filter = val; break;
                    case "prefix": prefix = val; break;
                    case "output.extension": outputExtension = val; break;
                    case "output.path": outputPath = val; break;
                    case "output.resize": resize = int.Parse(val); break;
                    case "css.path": cssPath = val; break;
                }
            }

            if (!outputPath.EndsWith("\\"))
            {
                outputPath += "\\";
            }

            if (!cssPath.EndsWith("\\"))
            {
                cssPath += "\\";
            }

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            if (!Directory.Exists(cssPath))
            {
                Directory.CreateDirectory(cssPath);
            }

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

            var packer = new RectanglePacker<string>();
            int count = 0;

            int avgWidth = 0;
            int avgHeight = 0;

            foreach (var file in files)
            {
                var img = new Bitmap(Bitmap.FromFile(file));

                if (resize != 0)
                {
                    img = new Bitmap(img, new Size(resize, resize));
                }

                images[file] = img;
                packer.AddRect(img.Width, img.Height, file);
                count++;

                avgWidth += img.Width;
                avgHeight += img.Height;
            }
            avgWidth /= count;
            avgHeight /= count;

            int side = (int)Math.Ceiling(Math.Sqrt(count));

            int atlasWidth = avgWidth * side;
            int atlasHeight = avgHeight * side;

            int tot = packer.Pack(0, 0, atlasWidth, atlasHeight);

            if (tot == 0)
            {
                var output = new Bitmap(atlasWidth, atlasHeight);

                using (Graphics g = Graphics.FromImage(output))
                {
                    foreach (var file in files)
                    {
                        int x, y;
                        packer.GetRect(file, out x, out y);

                        var img = images[file];
                        g.DrawImage(img, x, y, img.Width, img.Height);

                        Console.WriteLine("Merged " + file);
                    }
                }

                output.Save(outputPath + outPicName);

                Console.WriteLine("Generated " + outputPath + outPicName);

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
            else
            {
                Console.WriteLine("Failed packing...");
            }
        }
    }
}
