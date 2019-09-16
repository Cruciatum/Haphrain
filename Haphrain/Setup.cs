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
                if (File.Exists(Constants._WORKDIR_ + $"{Constants.slashType}Data{Constants.slashType}BotSettings.json"))
                {
                    bSettings = new BotSettings(Constants._WORKDIR_ + $"{Constants.slashType}Data{Constants.slashType}BotSettings.json");
                }
                else
                {
                    if (!Directory.Exists(Constants._WORKDIR_ +$"{Constants.slashType}Data"))
                    {
                        Directory.CreateDirectory(Constants._WORKDIR_ + $"{Constants.slashType}Data");
                    }
                    using (var response = await dbClient.Files.DownloadAsync("/Data/BotSettings.json"))
                    {
                        var f = File.Create(Constants._WORKDIR_ + $"{Constants.slashType}Data{Constants.slashType}BotSettings.json");
                        using (var rw = new StreamWriter(f))
                        {
                            Console.WriteLine($"Creating file {Constants._WORKDIR_ + $"{Constants.slashType}Data{Constants.slashType}BotSettings.json"}");
                            rw.Write(await response.GetContentAsStringAsync());
                        }
                    }
                    bSettings = new BotSettings(Constants._WORKDIR_ + $"{Constants.slashType}Data{Constants.slashType}BotSettings.json");
                }

                if (File.Exists(Constants._WORKDIR_ + $"{Constants.slashType}Data{Constants.slashType}DBSettings.json"))
                {
                    dbSettings = new DBSettings(Constants._WORKDIR_ + $"{Constants.slashType}Data{Constants.slashType}DBSettings.json");
                }
                else
                {
                    if (!Directory.Exists(Constants._WORKDIR_ + $"{Constants.slashType}Data"))
                    {
                        Directory.CreateDirectory(Constants._WORKDIR_ + $"{Constants.slashType}Data");
                    }
                    using (var response = await dbClient.Files.DownloadAsync("/Data/DBSettings.json"))
                    {
                        var f = File.Create(Constants._WORKDIR_ + $"{Constants.slashType}Data{Constants.slashType}DBSettings.json");
                        using (var rw = new StreamWriter(f))
                        {
                            Console.WriteLine($"Creating file {Constants._WORKDIR_ + $"{Constants.slashType}Data{Constants.slashType}DBSettings.json"}");
                            rw.Write(await response.GetContentAsStringAsync());
                        }
                    }
                    dbSettings = new DBSettings(Constants._WORKDIR_ + $"{Constants.slashType}Data{Constants.slashType}DBSettings.json");
                }
            }
            var r = new Result();
            r.botSettings = bSettings;
            r.dbSettings = dbSettings;
            return r;
        }
    }
}
