using Dropbox.Api;
using Haphrain.Classes.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Haphrain.Classes.JsonObjects;

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

        internal static async Task<Dictionary<string,Currency>> GetCurrencies(string APIKey)
        {
            Dictionary<string, Currency> updatedList = new Dictionary<string, Currency>();

            string WEBSERVICE_URL = string.Format("http://api.currencylayer.com/list?access_key={0}",APIKey);
            string jsonResponse = "";
            CurrencyDefinition cd = new CurrencyDefinition();

            try
            {
                var webRequest = WebRequest.Create(WEBSERVICE_URL);
                if (webRequest != null)
                {
                    webRequest.Method = "GET";
                    webRequest.Timeout = 12000;
                    webRequest.ContentType = "application/json";


                    using (Stream s = webRequest.GetResponse().GetResponseStream())
                    {
                        using (StreamReader sr = new StreamReader(s))
                        {
                            jsonResponse = sr.ReadToEnd();
                            cd = JsonConvert.DeserializeObject<CurrencyDefinition>(jsonResponse);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await LogWriter.WriteLogFile($"ERROR: Exception thrown : {ex.Message}");
                await LogWriter.WriteLogFile($"{ex.StackTrace}");
                Console.WriteLine($"Exception: {ex.Message}");
            }

            WEBSERVICE_URL = string.Format("http://api.currencylayer.com/live?access_key={0}&source={1}", APIKey,"USD");
            CurrencyConversionList ccl = new CurrencyConversionList();

            try
            {
                var webRequest = WebRequest.Create(WEBSERVICE_URL);
                if (webRequest != null)
                {
                    webRequest.Method = "GET";
                    webRequest.Timeout = 12000;
                    webRequest.ContentType = "application/json";


                    using (Stream s = webRequest.GetResponse().GetResponseStream())
                    {
                        using (StreamReader sr = new StreamReader(s))
                        {
                            jsonResponse = sr.ReadToEnd();
                            ccl = JsonConvert.DeserializeObject<CurrencyConversionList>(jsonResponse);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await LogWriter.WriteLogFile($"ERROR: Exception thrown : {ex.Message}");
                await LogWriter.WriteLogFile($"{ex.StackTrace}");
                Console.WriteLine($"Exception: {ex.Message}");
            }

            foreach (string s in cd.Currencies.Keys)
            {
                updatedList.Add(s, new Currency() { FullName = cd.Currencies[s], ValueInUSD = 0d });
            }

            foreach (string s in ccl.Quotes.Keys)
            {
                updatedList[s.Substring(3,3)].ValueInUSD = ccl.Quotes[s];
            }

            return updatedList;
        }
    }
}
