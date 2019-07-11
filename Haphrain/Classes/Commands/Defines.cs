using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;
using Haphrain.Classes.Data;
using Newtonsoft.Json;
using System.Net;
using Haphrain.Classes.JsonObjects;
using Discord;
using System.Linq;

namespace Haphrain.Classes.Commands
{
    public class Defines : ModuleBase<SocketCommandContext>
    {
        private const string _OXFORDKEY_ = Constants._OXFORDAPIKEY_;
        private const string _OXFORDID_ = Constants._OXFORDID_;

        [Command("define"), Alias("def"), Summary("Gets the definition of the provided term from Oxford Dictionary"), Priority(1)]
        public async Task OxfordDefine(params string[] term)
        {
            string WEBSERVICE_URL = "https://od-api.oxforddictionaries.com:443/api/v2/lemmas/en/" + string.Join(' ', term).ToLower();
            string jsonResponse = "";
            OxfordEntry oxfEntry = new OxfordEntry();
            OxfordLemma oxfLemma = new OxfordLemma();
            EmbedBuilder builder = new EmbedBuilder { Title = "Oxford Dictionary Definitions" };

            try
            {
                var webRequest = WebRequest.Create(WEBSERVICE_URL);
                if (webRequest != null)
                {
                    webRequest.Method = "GET";
                    webRequest.Timeout = 12000;
                    webRequest.ContentType = "application/json";
                    webRequest.Headers.Add("app_id", _OXFORDID_);
                    webRequest.Headers.Add("app_key", _OXFORDKEY_);

                    using (Stream s = webRequest.GetResponse().GetResponseStream())
                    {
                        using (StreamReader sr = new StreamReader(s))
                        {
                            jsonResponse = sr.ReadToEnd();
                            oxfLemma = JsonConvert.DeserializeObject<OxfordLemma>(jsonResponse);
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

            if (oxfLemma.Results != null){
                string newTerm = oxfLemma.Results.First(lex => lex.LexicalEntries[0].Text != null).LexicalEntries[0].Text.ToLower();
                WEBSERVICE_URL = "https://od-api.oxforddictionaries.com:443/api/v2/entries/en-gb/" + newTerm;

                try
                {
                    var webRequest = WebRequest.Create(WEBSERVICE_URL);
                    if (webRequest != null)
                    {
                        webRequest.Method = "GET";
                        webRequest.Timeout = 12000;
                        webRequest.ContentType = "application/json";
                        webRequest.Headers.Add("app_id", _OXFORDID_);
                        webRequest.Headers.Add("app_key", _OXFORDKEY_);

                        using (Stream s = webRequest.GetResponse().GetResponseStream())
                        {
                            using (StreamReader sr = new StreamReader(s))
                            {
                                jsonResponse = sr.ReadToEnd();
                                oxfEntry = JsonConvert.DeserializeObject<OxfordEntry>(jsonResponse);
                                var actualRes = oxfEntry.Results[0].LexicalEntries.First(e => e.Entries[0].Senses[0].Definitions != null).Entries[0].Senses[0];
                                builder.AddField(newTerm, actualRes.Definitions[0]);
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
            }

            if (builder.Fields.Count == 0)
            {
                await Context.Channel.SendMessageAsync("Failed to find this on the Oxford Dictionary, defaulting to Urban Dictionary");
                await UrbDefine(term);
                return;
            }
            else await Context.Channel.SendMessageAsync(null, false, builder.Build());
        }

        [Command("define urb"), Alias("def urb"), Summary("Gets the definition of the provided term from Urban Dictionary"), Priority(2)]
        public async Task UrbDefine(params string[] term)
        {
            string WEBSERVICE_URL = "http://api.urbandictionary.com/v0/define?result_type=exact&term=" + string.Join(' ', term);
            string jsonResponse = "";
            EmbedBuilder builder = new EmbedBuilder { Title = "Urban Dictionary Definition" };
            UrbDicJsonObject array = new UrbDicJsonObject();

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
                            array = JsonConvert.DeserializeObject<UrbDicJsonObject>(jsonResponse);
                        }
                    }
                }
                array.list = array.list.Where(i => i.word.ToLower().Contains(string.Join(' ', term).ToLower())).ToArray();

                var sortedArray = array.list.OrderBy(i => i.thumbs_up - i.thumbs_down).ToArray();
                for (int i = 0; i < 5; i++)
                {
                    if (sortedArray[i].definition != "")
                    {
                        if (sortedArray[i].definition.Length > 1000)
                        {
                            builder.AddField($"#{1 + i}: {sortedArray[i].word}", sortedArray[i].definition.Substring(0, 1000) + " (...)");
                        }
                        else builder.AddField($"#{1 + i}: {sortedArray[i].word}", sortedArray[i].definition);
                    }
                }
            }
            catch (Exception ex)
            {
                await LogWriter.WriteLogFile($"ERROR: Exception thrown : {ex.Message}");
                await LogWriter.WriteLogFile($"{ex.StackTrace}");
                Console.WriteLine($"Exception: {ex.Message}");
            }

            if (builder.Fields.Count == 0) { builder.AddField($"{string.Join(' ', term)}", "No good definitions found"); }
            await Context.Channel.SendMessageAsync(null, false, builder.Build());
        }
    }
}