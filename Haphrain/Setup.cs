using Dropbox.Api;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Haphrain
{
    internal static class Setup
    {
        internal struct Result
        {
            internal BotSettings botSettings;
            internal DBSettings dbSettings;
        }
        internal static async Task<Result> GetFiles(BotSettings bSettings, DBSettings dbSettings)
        {
            using (var dbClient = new DropboxClient(Constants._DBTOKEN_))
            {
                if (File.Exists(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data\BotSettings.json")))
                {
                    bSettings = new BotSettings(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data\BotSettings.json"));
                }
                else
                {
                    if (!Directory.Exists(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data")))
                    {
                        Directory.CreateDirectory(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data"));
                    }
                    using (var response = await dbClient.Files.DownloadAsync("/Data/BotSettings.json"))
                    {
                        var f = File.Create(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data\BotSettings.json"));
                        using (var rw = new StreamWriter(f))
                        {
                            Console.WriteLine($"Creating file {Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data\BotSettings.json")}");
                            rw.Write(await response.GetContentAsStringAsync());
                        }
                    }
                    bSettings = new BotSettings(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data\BotSettings.json"));
                }

                if (File.Exists(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data\DBSettings.json")))
                {
                    dbSettings = new DBSettings(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data\DBSettings.json"));
                }
                else
                {
                    if (!Directory.Exists(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data")))
                    {
                        Directory.CreateDirectory(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data"));
                    }
                    using (var response = await dbClient.Files.DownloadAsync("/Data/DBSettings.json"))
                    {
                        var f = File.Create(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data\DBSettings.json"));
                        using (var rw = new StreamWriter(f))
                        {
                            Console.WriteLine($"Creating file {Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data\DBSettings.json")}");
                            rw.Write(await response.GetContentAsStringAsync());
                        }
                    }
                    dbSettings = new DBSettings(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data\DBSettings.json"));
                }

                //if (Environment.OSVersion.Platform == PlatformID.Unix)
                //{
                //    if (!File.Exists("/home/vcap/app/home/vcap/deps/0/lib/libdb2.so"))
                //    {

                //        if (!Directory.Exists("/home")) Directory.CreateDirectory("/home");
                //        if (!Directory.Exists("/home/vcap")) Directory.CreateDirectory("/home/vcap");
                //        if (!Directory.Exists("/home/vcap/app")) Directory.CreateDirectory("/home/vcap/app");
                //        if (!Directory.Exists("/home/vcap/app/home")) Directory.CreateDirectory("/home/vcap/app/home");
                //        if (!Directory.Exists("/home/vcap/app/home/vcap")) Directory.CreateDirectory("/home/vcap/app/home/vcap");
                //        if (!Directory.Exists("/home/vcap/app/home/vcap/deps")) Directory.CreateDirectory("/home/vcap/app/home/vcap/deps");
                //        if (!Directory.Exists("/home/vcap/app/home/vcap/deps/0")) Directory.CreateDirectory("/home/vcap/app/home/vcap/deps/0");
                //        if (!Directory.Exists("/home/vcap/app/home/vcap/deps/0/lib")) Directory.CreateDirectory("/home/vcap/app/home/vcap/deps/0/lib");

                //        var list = dbClient.Files.ListFolderAsync("/lib", true, false, false, false, true);
                //        list.Wait();
                //        var resultingList = list.Result;
                //        var baseDir = "/home/vcap/app/home/vcap/deps/0";

                //        foreach (var i in resultingList.Entries)
                //        {
                //            Console.WriteLine($"{(i.IsFile ? "File" : "Directory")} found: {Path.GetDirectoryName(i.PathDisplay).ToLower()}/{i.Name}");
                //        }

                //        foreach (var dbxFolder in resultingList.Entries.Where(i => i.IsFolder))
                //        {
                //            Directory.CreateDirectory(baseDir + Path.GetDirectoryName(dbxFolder.PathDisplay).ToLower());
                //            Console.WriteLine("Downloaded directory " + baseDir + Path.GetDirectoryName(dbxFolder.PathDisplay).ToLower());

                //        }
                //        foreach (var dbxFile in resultingList.Entries.Where(i => i.IsFile))
                //        {
                //            using (var response = await dbClient.Files.DownloadAsync(Path.GetDirectoryName(dbxFile.PathDisplay).ToLower() + "/" + dbxFile.Name.ToLower()))
                //            {
                //                var f = File.Create(baseDir + Path.GetDirectoryName(dbxFile.PathDisplay).ToLower() + "/" + dbxFile.Name.ToLower());
                //                using (var rw = new StreamWriter(f))
                //                {
                //                    rw.Write(await response.GetContentAsStringAsync());
                //                    Console.WriteLine("Downloaded file " + baseDir + Path.GetDirectoryName(dbxFile.PathDisplay).ToLower() + "/" + dbxFile.Name.ToLower());
                //                }
                //            }
                //        }
                //    }
                //}
            }
            var r = new Result();
            r.botSettings = bSettings;
            r.dbSettings = dbSettings;
            return r;
        }
    }
}
