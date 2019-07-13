using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;

using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Haphrain.Classes.HelperObjects;
using System.Net;
using Haphrain.Classes.Data;
using Newtonsoft.Json;
using Haphrain.Classes.JsonObjects;
using System.Timers;
using Haphrain.Classes.Commands;
using System.Net.Http;

namespace Haphrain
{
    class Program
    {
        private DiscordSocketClient Client;
        private CommandService Commands;
        private IServiceProvider Provider;
        private static readonly HttpClient httpClient = new HttpClient();

        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug
            });

            Commands = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async,
                LogLevel = LogSeverity.Debug
            });

            Provider = new ServiceCollection()
                .AddSingleton(Client)
                .AddSingleton(Commands)
                .BuildServiceProvider();

            Client.MessageReceived += Client_MessageReceived;
            await Commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);

            Client.Ready += Client_Ready;
            Client.Log += Client_Log;
            Client.JoinedGuild += Client_JoinedGuild;
            Client.LeftGuild += Client_LeftGuild;
            Client.ReactionAdded += Client_ReactionAdded;

            string Token = "";
            using (var s = new FileStream((Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).Replace(@"bin\Debug\netcoreapp2.1", @"Data\Token.txt"), FileMode.Open, FileAccess.Read))
            {
                using (var r = new StreamReader(s))
                {
                    Token = r.ReadToEnd();
                }
            }
            if (!Directory.Exists(LogWriter.LogFileLoc.Replace(@"Logs\Log", @"Logs\"))) Directory.CreateDirectory(LogWriter.LogFileLoc.Replace(@"Logs\Log", @"Logs\"));

            await Client.LoginAsync(TokenType.Bot, Token);
            await Client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var tMsg = GlobalVars.TrackedLogChannelMessages.SingleOrDefault(m => m.SourceMessage.Id == reaction.MessageId && m.TriggerById == reaction.UserId);
            var guildID = ((SocketGuildChannel)channel).Guild.Id;
            if (tMsg != null)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location).Replace(@"bin\Debug\netcoreapp2.1", @"Data\Guilds.xml"));
                var guildNode = doc.SelectSingleNode($"/Guilds/Guild[@GuildID='{guildID}']");
                var optionsNode = guildNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "Options");
                var channelNode = optionsNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "LogChannelID");
                if (reaction.Emote.Name == "✅")
                {
                    await reaction.Channel.SendMessageAsync($"{reaction.User.Value.Mention}: This channel ({MentionUtils.MentionChannel(channel.Id)}) is now your log channel.");
                    //Change channel topic
                    channelNode.InnerText = channel.Id.ToString();
                    doc.Save(GlobalVars.GuildsFileLoc);
                }
                else if (reaction.Emote.Name == "🚫")
                {
                    if (channelNode.InnerText == "0")
                        await reaction.Channel.SendMessageAsync($"{reaction.User.Value.Mention}: Please make a channel for logging and run the command again there.");
                    else
                        await reaction.Channel.SendMessageAsync($"Logging channel has not been changed.");
                }
            }
            else
            {
                tMsg = GlobalVars.TrackedSettingsMessages.SingleOrDefault(m => m.SourceMessage.Id == reaction.MessageId && m.TriggerById == reaction.UserId);
                if (tMsg != null)
                {
                    Options guildOptions = new Options();
                    GlobalVars.GuildsFile.Load(GlobalVars.GuildsFileLoc);
                    var guildNode = GlobalVars.GuildsFile.SelectSingleNode($"/Guilds/Guild[@GuildID='{guildID}']");
                    var prefixNode = guildNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "Prefix");
                    var optionsNode = guildNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "Options");
                    guildOptions.LogEmbeds = (optionsNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "LogEmbeds").InnerText == "0") ? false : true;
                    guildOptions.LogAttachments = (optionsNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "LogAttachments").InnerText == "0") ? false : true;
                    if (reaction.Emote.Name == "\u0031\u20E3")
                    {
                        optionsNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "LogEmbeds").InnerText = guildOptions.LogEmbeds ? "0" : "1";
                        if (!guildOptions.LogEmbeds)
                        {
                            await channel.SendMessageAsync("Now logging messages with embeds.");
                        }
                        else await channel.SendMessageAsync("No longer logging messages with embeds.");
                    }
                    else if (reaction.Emote.Name == "\u0032\u20E3")
                    {
                        optionsNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "LogAttachments").InnerText = guildOptions.LogAttachments ? "0" : "1";
                        if (!guildOptions.LogAttachments)
                        {
                            await channel.SendMessageAsync("Now logging messages with attachments.");
                        }
                        else await channel.SendMessageAsync("No longer logging messages with attachments.");
                    }
                    GlobalVars.GuildsFile.Save(GlobalVars.GuildsFileLoc);
                }
                else
                {
                    //Check other trackings
                }
            }
            if (tMsg != null) await GlobalVars.UntrackMessage(tMsg);
        }

        private async Task CheckGuildsStartup()
        {
            GlobalVars.GuildsFile.Load(GlobalVars.GuildsFileLoc);
            var root = GlobalVars.GuildsFile.DocumentElement;
            foreach (SocketGuild g in Client.Guilds)
            {
                if (GlobalVars.GuildsFile.SelectSingleNode($"/Guilds/Guild[@GuildID='{g.Id}']") == null)
                {
                    await Client_JoinedGuild(g);
                }
            }
        }

        private async Task Client_LeftGuild(SocketGuild arg)
        {
            Console.WriteLine($"{DateTime.Now} -> Left guild: {arg.Id}");

            GlobalVars.GuildsFile.Load(GlobalVars.GuildsFileLoc);
            var root = GlobalVars.GuildsFile.DocumentElement;
            var guildNode = GlobalVars.GuildsFile.SelectSingleNode($"/Guilds/Guild[@GuildID='{arg.Id}']");

            root.RemoveChild(guildNode);

            GlobalVars.GuildsFile.Save(GlobalVars.GuildsFileLoc);

            await UpdateActivity();
            await Task.Delay(100);

        }

        private async Task Client_JoinedGuild(SocketGuild arg)
        {
            Console.WriteLine($"{DateTime.Now} -> Joined guild: {arg.Id}");

            GlobalVars.GuildsFile.Load(GlobalVars.GuildsFileLoc);
            //add new guildobject to Guilds file
            var root = GlobalVars.GuildsFile.DocumentElement;

            var guildNode = GlobalVars.GuildsFile.CreateElement("Guild");
            var guildID = GlobalVars.GuildsFile.CreateAttribute("GuildID");
            var prefixNode = GlobalVars.GuildsFile.CreateElement("Prefix");
            var nameNode = GlobalVars.GuildsFile.CreateElement("GuildName");
            var ownerID = GlobalVars.GuildsFile.CreateElement("OwnerID");
            var optionsNode = GlobalVars.GuildsFile.CreateElement("Options");
            var optionLogEmbed = optionsNode.AppendChild(GlobalVars.GuildsFile.CreateElement("LogEmbeds")).InnerText = "0";
            var optionLogAttachments = optionsNode.AppendChild(GlobalVars.GuildsFile.CreateElement("LogAttachments")).InnerText = "0";

            var optionLogChannelID = optionsNode.AppendChild(GlobalVars.GuildsFile.CreateElement("LogChannelID")).InnerText = "0";

            guildID.Value = arg.Id.ToString();
            guildNode.Attributes.Append(guildID);

            nameNode.InnerText = arg.Name;
            guildNode.AppendChild(nameNode);

            ownerID.InnerText = arg.Owner.Id.ToString();
            guildNode.AppendChild(ownerID);

            prefixNode.InnerText = "]";
            guildNode.AppendChild(prefixNode);

            guildNode.AppendChild(optionsNode);

            root.AppendChild(guildNode);

            GlobalVars.GuildsFile.Save(GlobalVars.GuildsFileLoc);

            await UpdateActivity();
            await Task.Delay(100);
        }

        private async Task Client_Log(LogMessage arg)
        {
            if (arg.Severity <= (GlobalVars.GuildsFileLoc.Contains("Live") ? LogSeverity.Info : LogSeverity.Debug))
            {
                if (arg.Exception != null)
                {
                    Console.WriteLine($"EXCEPTION [{arg.Severity.ToString()}]: {DateTime.Now} at {arg.Exception.Source} -> {arg.Exception.Message}");
                }
                Console.WriteLine($"[{arg.Severity.ToString().ToUpper()}]: {DateTime.Now} at {arg.Source} -> {arg.Message}");
            }
            if (arg.Exception != null)
            {
                await LogWriter.WriteLogFile($"EXCEPTION [{arg.Severity.ToString()}]: {DateTime.Now} {arg.Exception.Source} -> {arg.Exception.Message}");
            }
            await LogWriter.WriteLogFile($"[{arg.Severity.ToString().ToUpper()}]: {DateTime.Now} at {arg.Source} -> {arg.Message}");
        }

        private async Task Client_Ready()
        {
            await UpdateActivity();
            await CheckGuildsStartup();
            await Client_Log(new LogMessage(LogSeverity.Info, "Client_Ready", "Bot ready!"));
        }

        private async Task Client_MessageReceived(SocketMessage arg)
        {
            var msg = arg as SocketUserMessage;
            if (msg.Content.Length <= 1 && msg.Embeds.Count == 0 && msg.Attachments.Count == 0) return;
            var guildOptions = new Options();

            var context = new SocketCommandContext(Client, msg);

            if ((context.Message == null || context.Message.Content == "") && arg.Attachments.Count == 0 && arg.Embeds.Count == 0) return;
            if (context.User.IsBot) return;

            int argPos = 0;
            string guildPrefix = "]";

            GlobalVars.GuildsFile.Load(GlobalVars.GuildsFileLoc);
            var guildNode = GlobalVars.GuildsFile.SelectSingleNode($"/Guilds/Guild[@GuildID='{context.Guild.Id}']");
            var prefixNode = guildNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "Prefix");
            var optionsNode = guildNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "Options");
            guildOptions.LogEmbeds = (optionsNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "LogEmbeds").InnerText == "0") ? false : true;
            guildOptions.LogAttachments = (optionsNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "LogAttachments").InnerText == "0") ? false : true;
            guildOptions.LogChannelID = ulong.Parse(optionsNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "LogChannelID").InnerText);

            if (guildOptions.LogChannelID != 0)
            {
                if (guildOptions.LogEmbeds)
                    if (msg.Embeds.Count > 0) { await ImageLogger.LogEmbed(msg, guildOptions.LogChannelID, Client); }
                if (guildOptions.LogAttachments)
                    if (msg.Attachments.Count > 0) { await ImageLogger.LogAttachment(msg, guildOptions.LogChannelID, Client); }
            }

            if (prefixNode != null) guildPrefix = prefixNode.InnerText;

            if (!(msg.HasStringPrefix(guildPrefix, ref argPos)) && !(msg.HasMentionPrefix(Client.CurrentUser, ref argPos))) return;

            if (!(await GlobalVars.CheckUserTimeout(context.Message.Author, context.Guild.Id, context.Channel))) return;
            IResult Result = null;
            try
            {
                Result = await Commands.ExecuteAsync(context, argPos, Provider);
                if (Result.Error == CommandError.UnmetPrecondition)
                {
                    var errorMsg = await context.Channel.SendMessageAsync(Result.ErrorReason);
                    GlobalVars.AddRandomTracker(errorMsg);
                }
                else if (!Result.IsSuccess)
                {
                    if (Result.ErrorReason.ToLower().Contains("unknown command"))
                    {
                        await Client_Log(new LogMessage(LogSeverity.Error, "Client_MessageReceived", $"Unknown command sent by {context.Message.Author.ToString()} in guild: {context.Guild.Id} - Command text: {context.Message.Content}"));
                        var errorMsg = await context.Channel.SendMessageAsync($"Sorry, I don't know what I'm supposed to do with that...");
                        GlobalVars.AddRandomTracker(errorMsg);
                    }
                    else
                        await Client_Log(new LogMessage(LogSeverity.Error, "Client_MessageReceived", $"Command text: {context.Message.Content} | Error: {Result.ErrorReason}"));
                }
                GlobalVars.AddUserTimeout(context.Message.Author, context.Guild.Id);
            }
            catch (Exception ex)
            {
                await Client_Log(new LogMessage(LogSeverity.Critical, context.Message.Content, Result.ErrorReason, ex));
            }
        }

        private async Task UpdateActivity()
        {
            string activity = "";
            using (var s = new FileStream((Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).Replace(@"bin\Debug\netcoreapp2.1", @"Data\Activity.txt"), FileMode.Open, FileAccess.Read))
            {
                using (var r = new StreamReader(s))
                {
                    activity = r.ReadToEnd();
                }
            }
            string version = "";
            using (var s = new FileStream((Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).Replace(@"bin\Debug\netcoreapp2.1", @"Data\Version.txt"), FileMode.Open, FileAccess.Read))
            {
                using (var r = new StreamReader(s))
                {
                    version = r.ReadToEnd();
                }
            }

            activity = activity.Replace("{serverCount}", Client.Guilds.Count.ToString());
            await Client.SetGameAsync($"{activity} {version}");
        }
    }
}