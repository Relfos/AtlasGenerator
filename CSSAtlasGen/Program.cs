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
        // example args: --folder=D:\code\Sites\Phantasma_Site\team --filter=*.jpg --prefix=team -output=jpg --resize=240
        static void Main(string[] args)
        {
            string folder = null;
            string filter = "*.*";
            string outputExtension = "jpg";
            string outputPrefix = null;
            int resize = 0;

            foreach (var entry in args)
            {
                var arg = entry;

                if (!arg.StartsWith("--"))
                {
                    Console.WriteLine("Invalid argument: " + arg);
                    return;
                }

                arg = arg.Substring(2);

                var temp = arg.Split(new char[] { '=' }, 2);

                var key = temp[0].ToLower();
                var val = temp.Length == 2 ? temp[1] : null;

                switch (key)
                {
                    case "folder": folder = val; break;
                    case "filter": filter = val; break;
                    case "prefix": outputPrefix = val; break;
                    case "output": outputExtension = val; break;
                    case "resize": resize = int.Parse(val); break;
                }
            }

            if (folder == null)
            {
                Console.WriteLine("Please specify a folder. Eg: --folder=some_path");
                return;
            }

            if (outputPrefix == null)
            {
                Console.WriteLine("Please specify a atlas prefix. Eg: --prefix=something");
                return;
            }

            var outPicName = outputPrefix + "." + outputExtension;
            var outCSSName = outputPrefix + ".css";

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
                    }
                }

                output.Save(outPicName);

                Console.WriteLine("Generated " + outPicName);

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

                    sb.Append($".{outputPrefix}-{name}");

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
                    sb.AppendLine("." + outputPrefix + "-" + name + " {");
                    sb.AppendLine($"\twidth: {img.Width}px;");
                    sb.AppendLine($"\theight: {img.Height}px;");
                    sb.AppendLine($"\tbackground-position: -{x}px -{y}px;");
                    sb.Append('}');
                    sb.AppendLine();
                }

                File.WriteAllText(outCSSName, sb.ToString());

                Console.WriteLine("Generated " + outCSSName);
            }
            else
            {
                Console.WriteLine("Failed packing...");
            }
        }
    }
}
