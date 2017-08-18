using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.IO;

using Mono.Options;

using StegoService.Core.Blocks;
using StegoService.Core.Helpers;
using StegoService.Core.BitmapContainer;

namespace BenhamStego
{
    class Program
    {
        private static string inputFile = null;
        private static string textFile = null;
        private static string outputFile = null;
        private static bool extract = false;
        private static bool embed = false;
        private static bool noise = false;
        private static bool help = false;

        public static OptionSet options = new OptionSet
        {
            { "e|embed",    "Embed the message.",      (flag) => embed = flag != null },
            { "x|extract",  "Extract the message.",    (flag) => extract = flag != null },
            { "n|noise",    "Add noise.",              (flag) => noise = flag != null },
            { "h|help",     "Print this message.",     (flag) => help = flag != null },
            { "f|file=",    "Input file.",             (string str) => inputFile = str },
            { "o|output=",  "Output file.",            (string str) => outputFile = str },
            { "t|txt=",     "File with text.",         (string str) => textFile = str }
        };

        public void PrintHelp()
        {
            Console.WriteLine("USAGE:");
            Console.WriteLine("  stego.exe -ef image.bmp [-n] -t text.txt  -o result.bmp");
            Console.WriteLine("            -xf image.bmp -o result.txt");
            Console.WriteLine();
            options.WriteOptionDescriptions(Console.Out);
        }

        public static void Main(string[] args)
        {
            var program = new Program();
            options.Parse(args);
            program.Run();
        }

        public void Run()
        {
            if ((extract == embed) || help || extract && noise)
            {
                PrintHelp();
            }
            else
            {
                try
                {
                    var bitmap = new Bitmap(inputFile);
                    var bitmapContainer = new BitmapContainer(bitmap);
                    if (extract)
                    {
                        string text = bitmapContainer.ExtractStego();
                        using (var fileStream = File.Open(outputFile, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            var streamWriter = new StreamWriter(fileStream, Encoding.UTF8);
                            streamWriter.Write(text);
                        }
                    }
                    else if (embed)
                    {
                        var fileStream = File.Open(textFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                        var streamReader = new StreamReader(fileStream, Encoding.UTF8);
                        var text = streamReader.ReadToEnd();
                        if (noise)
                        {
                            bitmapContainer.AddNoise(ArgbChannels.Blue);
                        }
                        bitmapContainer.EmbedStego(text);
                        bitmapContainer.Bitmap.Save(outputFile);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: " + e.Message);
                    Console.WriteLine();
                    PrintHelp();
                }
            }
        }
    }
}
