using System.IO.Compression;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

namespace CurseforgeZipDownloader
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var cwd = Directory.GetCurrentDirectory();
            Console.WriteLine($"Searching zip file in \"{cwd}\".");

            var files = Directory.GetFiles(cwd).Select(x => new FileInfo(x)).ToList();
            var zipFiles = files.Where(x => x.Extension == ".zip").ToList();

            if (zipFiles.Count == 0)
            {
                Console.WriteLine($"Found no zip file in \"{cwd}\". Exiting.");
                Environment.Exit(0);
            }
            if (zipFiles.Count > 1)
            {
                Console.WriteLine($"Found multiple zip files in \"{cwd}\". Exiting.");
                Environment.Exit(0);
            }

            var zipPath = zipFiles.First().FullName;
            var folderPath = zipFiles.First().FullName.Remove(zipPath.Length - 4, 4);
            
            if (Directory.Exists(folderPath))
                AskDelete(folderPath);

            Console.WriteLine($"Unzipping to \"{folderPath}\"");
            ZipFile.ExtractToDirectory(zipPath, folderPath);

            var htmlfile = Path.Combine(folderPath, "modlist.html");
            var content = File.ReadAllLines(htmlfile).ToList();

            content.RemoveAt(0);
            content.RemoveAt(content.Count - 1);

            var mods = new List<Tuple<string, string>>();

            for (int i = 0; i < content.Count; i++)
            {
                Match m = Regex.Match(content[i], @"^\<li\>\<a\shref\=""(?<url>[^""]+)""\>(?<name>[^\<]+)\<\/a\>\<\/li\>");
                mods.Add(new (m.Groups["url"].Value, m.Groups["name"].Value));
            }

            if (mods.Count != content.Count)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Unexpected Error: Found less mods that the file contains ({mods.Count} vs {content.Count}). Exiting.");
                Console.ResetColor();
                Environment.Exit(0);
            }

            var manifestfile = Path.Combine(folderPath, "manifest.json");
            var json = File.ReadAllText(manifestfile);
            var manifest = JsonConvert.DeserializeObject<Manifest>(json);

            if (manifest == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Unexpected Error: Could not convert manifest file \"{manifestfile}\". Exiting.");
                Console.ResetColor();
                Environment.Exit(0);
            }

            var modInfo = manifest.files;
            if (modInfo.Count != mods.Count)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Unexpected Error: Found less mods in the manifest file than in the html file ({modInfo.Count} vs {mods.Count}). Exiting.");
                Console.ResetColor();
                Environment.Exit(0);
            }

            for (int i = 0; i < mods.Count; i++)
            {
                mods[i] = new Tuple<string, string>($"https://www.curseforge.com/api/v1/mods/{modInfo[i].projectID}/files/{modInfo[i].fileID}", mods[i].Item2);
            }

            var modDir = Path.Combine(cwd, "mods");

            if (Directory.Exists(modDir))
                AskDelete(modDir);

            Directory.CreateDirectory(modDir);
            Directory.SetCurrentDirectory(modDir);

            using var client = new HttpClient();
            for (int i = 0; i < mods.Count; i++)
            {
                Console.WriteLine($"{i + 1}. download: \"{mods[i].Item2}\"");

                var response = await client.GetAsync(mods[i].Item1);
                var data = await response.Content.ReadAsStringAsync();

                var filename = Regex.Match(data, @"""fileName""\:""(?<filename>[^""]+)""");

                using var s = await client.GetStreamAsync(mods[i].Item1 + "/download");
                using var fs = new FileStream(filename.Groups["filename"].Value, FileMode.OpenOrCreate);
                await s.CopyToAsync(fs);
            }
        }

        static void AskDelete(string dir)
        {
            Console.Write($"Directory \"{dir}\" already exists.\nDelete? (y/n): ");
            var ans = Console.ReadLine() ?? string.Empty;
            if (ans.StartsWith('y'))
                Directory.Delete(dir, true);
            else
            {
                Console.WriteLine("Exiting.");
                Environment.Exit(0);
            }
        }
    }
}
