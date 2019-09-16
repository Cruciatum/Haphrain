using Discord;
using Discord.WebSocket;
using Haphrain.Classes.Commands;
using Haphrain.Classes.HelperObjects;
using Haphrain.Classes.JsonObjects;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;

namespace Haphrain.Classes.Data
{
    public static class ImageLogger
    {
        private static string GfyCatCredential = "";

        public static string GetTenorGIF(string url)
        {
            string gifURL = "";
            string id = url.Remove(0, url.LastIndexOf('-') + 1);
            string WEBSERVICE_URL = $"https://api.tenor.com/v1/gifs?key={Constants._TENORAPIKEY_}&ids={id}&media_filter=minimal";
            string jsonResponse;
            TenorResult obj = new TenorResult();

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
                            obj = JsonConvert.DeserializeObject<TenorResult>(jsonResponse);

                        }
                    }
                    gifURL = obj.results[0].media[0].gif.url;
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }

            return gifURL;
        }

        public static string GetGfyCatAsync(string url)
        {
            string gifURL = "";
            string WEBSERVICE_URL = $"https://api.gfycat.com/v1/gfycats/{url.Replace("https://gfycat.com/", "")}";
            if (WEBSERVICE_URL.Contains('-'))
            {
                WEBSERVICE_URL = WEBSERVICE_URL.Substring(0, WEBSERVICE_URL.IndexOf('-'));
            }
            string jsonResponse;
            GfyCatResult obj = new GfyCatResult();
            WebHeaderCollection headers = new WebHeaderCollection();
            if (GfyCatCredential == "")
            {
                string grant_type = "client_credentials";
                string authURL = "https://api.gfycat.com/v1/oauth/token";
                var result = "";

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(authURL);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                GfyCatAuthPost postData = new GfyCatAuthPost
                {
                    grant_type = grant_type,
                    client_id = Constants._GFYCATID_,
                    client_secret = Constants._GFYCATSECRET_
                };
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = JsonConvert.SerializeObject(postData);

                    streamWriter.Write(json);
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }

                var resultObj = JsonConvert.DeserializeObject<GfyCatAuthResult>(result);

                GfyCatCredential = "Bearer " + resultObj.access_token;

                Timer t = new Timer();
                void handler(object sender, ElapsedEventArgs e)
                {
                    t.Stop();
                    GfyCatCredential = "";
                }
                t.StartTimer(handler, (ulong)(resultObj.expires_in * 1000));
            }

            headers.Add("Authorization", GfyCatCredential);

            try
            {
                var webRequest = WebRequest.Create(WEBSERVICE_URL);
                if (webRequest != null)
                {
                    webRequest.Method = "GET";
                    webRequest.Headers = headers;
                    webRequest.Timeout = 12000;
                    webRequest.ContentType = "application/json";

                    using (Stream s = webRequest.GetResponse().GetResponseStream())
                    {
                        using (StreamReader sr = new StreamReader(s))
                        {
                            jsonResponse = sr.ReadToEnd();
                            obj = JsonConvert.DeserializeObject<GfyCatResult>(jsonResponse);
                        }
                    }

                }

                gifURL = obj.GfyItem.GifUrl;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }

            return gifURL;
        }

        public static async Task LogEmbed(SocketMessage msg, ulong logChannelID, DiscordSocketClient client)
        {
            Embed[] embeds = msg.Embeds.ToArray();
            var channel = client.GetChannel(logChannelID) as IMessageChannel;
            foreach (Embed e in embeds)
            {
                if (e.Type == EmbedType.Gifv || e.Type == EmbedType.Image)
                {
                    EmbedBuilder t = new EmbedBuilder();
                    t.ImageUrl = e.Url.Contains("tenor") ? GetTenorGIF(e.Url) : e.Url.Contains("gfycat") ? GetGfyCatAsync(e.Url) : e.Url;
                    await channel.SendMessageAsync($"From: {msg.Author.Mention}({msg.Author.Username}#{msg.Author.Discriminator}) in {MentionUtils.MentionChannel(msg.Channel.Id)}\nURL: {msg.GetJumpUrl()}", false, t.Build());
                }
            }
        }

        public static async Task LogAttachment(SocketMessage msg, ulong logChannelID, DiscordSocketClient client)
        {
            Attachment[] attached = msg.Attachments.ToArray();
            var channel = client.GetChannel(logChannelID) as IMessageChannel;
            string[] imgFileTypes = { ".jpg", ".jpeg", ".gif", ".png", ".webp" };
            string path = Constants._WORKDIR_ + $"{Constants.slashType}Images";

            foreach (Attachment a in attached)
            {
                string file = path + Constants.slashType + a.Filename;
                string ext = Path.GetExtension(a.Url);
                if (imgFileTypes.Contains(ext.ToLower()))
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    using (var c = new WebClient())
                    {
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                        }
                        c.DownloadFile(a.Url, file);
                        while (c.IsBusy) { }
                    }
                    if (msg.Content == "")
                        await channel.SendFileAsync(file, $"From: {msg.Author.Mention}({msg.Author.Username}#{msg.Author.Discriminator}) in {MentionUtils.MentionChannel(msg.Channel.Id)}\nURL: {msg.GetJumpUrl()}");
                    else
                        await channel.SendFileAsync(file, $"From: {msg.Author.Mention}({msg.Author.Username}#{msg.Author.Discriminator}) in {MentionUtils.MentionChannel(msg.Channel.Id)}\nIncluded message: {msg.Content}\nURL: {msg.GetJumpUrl()}");
                    File.Delete(file);
                }
                else
                {
                    await channel.SendMessageAsync($"From: {msg.Author.Mention}({msg.Author.Username}#{msg.Author.Discriminator}) in {MentionUtils.MentionChannel(msg.Channel.Id)}\n**Unrecognized Image type**: \"{Path.GetExtension(attached[0].Url).ToUpper()}\"\nURL: {msg.GetJumpUrl()}");
                }
            }
        }
    }
}
