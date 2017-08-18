using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using StegoService.Core.BitmapContainer;

using HttpMultipartParser;

using RedHttpServerNet45;

namespace StegoService.Web
{
    public class Server
    {
        public const string DefaultPublicFolder = @"static";
        public const string ResultsFolder = @"results";
        public const int DefaultPort = 80;

        public readonly string Folder;
        public readonly int Port;

        protected RedHttpServer m_server;

        public Server(int port = DefaultPort, string publicFolder = DefaultPublicFolder)
        {
            Folder = publicFolder;
            Port = port;
            m_server = new RedHttpServer(port, publicFolder);
            if (!Directory.Exists(ResultsFolder))
            {
                Directory.CreateDirectory(ResultsFolder);
            }
        }

        public static string GetUniqueName()
        {
            string name = Path.Combine(ResultsFolder, Guid.NewGuid().ToString() + ".png");
            while (File.Exists(name))
            {
                name = Path.Combine(ResultsFolder, Guid.NewGuid().ToString() + ".png");
            }
            return name;
        }

        public void Start()
        {
            m_server.Get("/", (req, res) =>
            {
                res.Redirect("index.html");
            });
            m_server.Post("/insert", async (req, res) =>
            {
                try
                {
                    string filename = GetUniqueName();
                    var stream = req.GetBodyStream();
                    await Task.Run(() =>
                    {
                        var parser = new MultipartFormDataParser(stream);
                        var file = parser.Files[0];
                        string text = parser.Parameters[0].Data;
                        bool noise = parser.Parameters[1].Data == "yes";
                        var bitmap = new Bitmap(file.Data);
                        var bitmapContainer = new BitmapContainer(bitmap);
                        bitmapContainer.EmbedStego(text);
                        if (noise)
                        {
                            bitmapContainer.AddNoise(ArgbChannels.Blue);
                        }
                        bitmapContainer.Bitmap.Save(filename, ImageFormat.Png);
                    });
                    await res.Download(filename);
                }
                catch (Exception e)
                {
                    await res.SendString(e.ToString());
                }
            });
            m_server.Post("/extract", async (req, res) =>
            {
                try
                {
                    string text = String.Empty;
                    var stream = req.GetBodyStream();
                    await Task.Run(() =>
                    {
                        var parser = new MultipartFormDataParser(stream);
                        var file = parser.Files[0];
                        var bitmap = new Bitmap(file.Data);
                        var bitmapContainer = new BitmapContainer(bitmap);
                        text = bitmapContainer.ExtractStego();
                    });
                    await res.SendString(text);
                }
                catch (Exception e)
                {
                    await res.SendString(e.ToString());
                }
            });
            m_server.Start();
        }

        public static void Main(string[] args)
        {
            var server = new Server();
            server.Start();
        }
    }
}
