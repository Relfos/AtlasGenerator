using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Lunar.Utils;

namespace CSSAtlasGen
{
    class Program
    {
        static void Main(string[] args)
        {
            var folder = @"D:\code\Sites\Phantasma_Site\team";
            var filter = "*.jpg";
            var resize = 240;
            var outputPrefix = "team";
            var outPicName = outputPrefix + ".jpg";
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
                    sb.AppendLine("."+outputPrefix+"-"+name+" {");
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
