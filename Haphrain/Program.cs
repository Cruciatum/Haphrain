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
using IBM.Data.DB2;
using IBM.Data.DB2.Core;
using IBM.Data.DB2Types;

namespace Haphrain
{
    class Program
    {
        private DiscordSocketClient Client;
        private CommandService Commands;
        private IServiceProvider Provider;
        private static readonly HttpClient httpClient = new HttpClient();
        private BotSettings bSettings = new BotSettings(Constants._WORKDIR_ + @"\Data\BotSettings.json");
        private DBSettings dbSettings = new DBSettings(Constants._WORKDIR_ + @"\Data\DBSettings.json");

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
            Client.ReactionRemoved += Client_ReactionRemoved;
            
            if (!Directory.Exists(LogWriter.LogFileLoc.Replace(@"Logs\Log", @"Logs\"))) Directory.CreateDirectory(LogWriter.LogFileLoc.Replace(@"Logs\Log", @"Logs\"));
            DBControl.dbSettings = dbSettings;

            await Client.LoginAsync(TokenType.Bot, bSettings.token);
            await Client.StartAsync();

            Timer t = new Timer();
            t.AutoReset = true;
            async void handler (object sender, ElapsedEventArgs e)
            {
                foreach (Poll p in GlobalVars.Polls)
                    await p.Update();
            }
            t.StartTimer(handler, 5000);

            await Task.Delay(-1);
        }

        private void GetSQLData()
        {
            //Load prefix & options from DB
            DB2ConnectionStringBuilder sBuilder = new DB2ConnectionStringBuilder();
            sBuilder.Database = dbSettings.db;
            sBuilder.UserID = dbSettings.username;
            sBuilder.Password = dbSettings.password;
            sBuilder.Server = dbSettings.host + ":" + dbSettings.port;
            DB2Connection conn = new DB2Connection();
            conn.ConnectionString = sBuilder.ConnectionString;

            
            using (conn)
            {
                conn.Open();

                #region Get Guilds
                DB2Command cmd = new DB2Command($"SELECT * FROM Guilds", conn);
                DB2DataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    GuildOption go = new GuildOption();
                    Options o = new Options();

                    go.GuildID = Convert.ToUInt64(dr.GetValue(0));
                    go.GuildName = Convert.ToString(dr.GetValue(1));
                    go.OwnerID = Convert.ToUInt64(dr.GetValue(2));
                    go.Prefix = Convert.ToString(dr.GetValue(3));
                    o.LogChannelID = Convert.ToUInt64(dr.GetValue(4));
                    o.LogEmbeds = Convert.ToBoolean(dr.GetValue(5));
                    o.LogAttachments = Convert.ToBoolean(dr.GetValue(6));

                    go.Options = o;

                    GlobalVars.GuildOptions.Add(go);
                }
                #endregion

                conn.Close();
                conn.Dispose();
            }
        }

        private async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            try
            {
                if (reaction.User.Value.Id == Client.CurrentUser.Id) return;
                var tMsg = GlobalVars.TrackedLogChannelMessages.SingleOrDefault(m => m.SourceMessage.Id == reaction.MessageId && m.TriggerById == reaction.UserId);
                var guildID = ((SocketGuildChannel)channel).Guild.Id;
                if (tMsg != null)
                {
                    if (reaction.Emote.Name == "✅")
                    {
                        GlobalVars.GuildOptions.Single(x => x.GuildID == guildID).Options.LogChannelID = channel.Id;
                        DBControl.UpdateDB($"UPDATE Guilds SET LogChannelID = {channel.Id.ToString()} WHERE GuildID = {guildID.ToString()};");
                        await reaction.Channel.SendMessageAsync($"{reaction.User.Value.Mention}: This channel ({MentionUtils.MentionChannel(channel.Id)}) is now your log channel.");
                        //Change channel topic
                    }
                    else if (reaction.Emote.Name == "🚫")
                    {
                        if (GlobalVars.GuildOptions.Single(x => x.GuildID == guildID).Options.LogChannelID == 0)
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
                        Options guildOptions = GlobalVars.GuildOptions.Single(x => x.GuildID == guildID).Options;
                        if (reaction.Emote.Name == "\u0031\u20E3")
                        {
                            guildOptions.LogEmbeds = !guildOptions.LogEmbeds;
                            DBControl.UpdateDB($"UPDATE Guilds SET LogEmbeds = {guildOptions.LogEmbeds} WHERE GuildID = {guildID};");
                            if (!guildOptions.LogEmbeds)
                            {
                                await channel.SendMessageAsync("Now logging messages with embeds.");
                            }
                            else await channel.SendMessageAsync("No longer logging messages with embeds.");
                        }
                        else if (reaction.Emote.Name == "\u0032\u20E3")
                        {
                            guildOptions.LogAttachments = !guildOptions.LogAttachments;
                            DBControl.UpdateDB($"UPDATE Guilds SET LogAttachments = {guildOptions.LogAttachments} WHERE GuildID = {guildID};");
                            if (!guildOptions.LogAttachments)
                            {
                                await channel.SendMessageAsync("Now logging messages with attachments.");
                            }
                            else await channel.SendMessageAsync("No longer logging messages with attachments.");
                        }
                    }
                    else
                    {
                        //Check other trackings
                    }
                }
                if (tMsg != null) { await GlobalVars.UntrackMessage(tMsg); return; }
                var p = GlobalVars.Polls.SingleOrDefault(x => x.PollMessage.Id == reaction.MessageId);
                if (p != null)
                {
                    bool? b = false;
                    if (p.PollReactions.SingleOrDefault(x => x.User.Id == reaction.User.Value.Id) == null)
                        b = p.AddReaction((SocketUser)reaction.User, p.PollOptions.SingleOrDefault(x => x.React.Name == reaction.Emote.Name).Option);
                    else
                        await p.PollMessage.RemoveReactionAsync(reaction.Emote, (SocketUser)reaction.User);
                    if (b != true)
                    {
                        await p.PollMessage.RemoveReactionAsync(reaction.Emote, (SocketUser)reaction.User);
                    }
                }
            }
            catch { }
        }

        private Task Client_ReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            try
            {
                var poll = GlobalVars.Polls.SingleOrDefault(x => x.PollMessage.Id == reaction.MessageId);
                if (poll != null)
                {
                    bool? b = poll.RemoveReaction((SocketUser)reaction.User, poll.PollOptions.SingleOrDefault(x => x.React.Name == reaction.Emote.Name).Option);
                }
            }
            catch { }
            return Task.CompletedTask;
        }

        private async Task CheckGuildsStartup()
        {
            foreach (SocketGuild g in Client.Guilds)
            {
                if (GlobalVars.GuildOptions.Where(x => x.GuildID == g.Id) == null)
                {
                    await Client_JoinedGuild(g);
                }
            }
        }

        private async Task Client_LeftGuild(SocketGuild arg)
        {
            Console.WriteLine($"{DateTime.Now} -> Left guild: {arg.Id}");

            GlobalVars.GuildOptions.Remove(GlobalVars.GuildOptions.Single(x => x.GuildID == arg.Id));

            DBControl.UpdateDB($"DELETE FROM Guilds WHERE GuildID = {arg.Id};");

            await UpdateActivity();
            await Task.Delay(100);

        }

        private async Task Client_JoinedGuild(SocketGuild arg)
        {
            Console.WriteLine($"{DateTime.Now} -> Joined guild: {arg.Id}");

            GuildOption go = new GuildOption();
            Options o = new Options();

            go.GuildID = arg.Id;
            go.GuildName = arg.Name;
            go.OwnerID = arg.Owner.Id;
            go.Prefix = "]";
            o.LogChannelID = 0;
            o.LogEmbeds = o.LogAttachments = false;

            go.Options = o;
            GlobalVars.GuildOptions.Add(go);

            DBControl.UpdateDB($"INSERT INTO Guilds VALUES ({go.GuildID.ToString()}, {go.GuildName},{go.OwnerID.ToString()},{go.Prefix},{go.Options.LogChannelID.ToString()}, {go.Options.LogEmbeds}, {go.Options.LogAttachments});");

            await UpdateActivity();
            await Task.Delay(100);
        }

        private async Task Client_Log(LogMessage arg)
        {
            if (arg.Severity <= (Constants._WORKDIR_.Contains("Live") ? LogSeverity.Info : LogSeverity.Debug))
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
            
            var context = new SocketCommandContext(Client, msg);
            var guildOptions = GlobalVars.GuildOptions.Single(x => x.GuildID == context.Guild.Id);

            if ((context.Message == null || context.Message.Content == "") && arg.Attachments.Count == 0 && arg.Embeds.Count == 0) return;
            if (context.User.IsBot) return;

            int argPos = 0;

            if (guildOptions.Options.LogChannelID != 0)
            {
                if (guildOptions.Options.LogEmbeds)
                    if (msg.Embeds.Count > 0) { await ImageLogger.LogEmbed(msg, guildOptions.Options.LogChannelID, Client); }
                if (guildOptions.Options.LogAttachments)
                    if (msg.Attachments.Count > 0) { await ImageLogger.LogAttachment(msg, guildOptions.Options.LogChannelID, Client); }
            }

            if (!(msg.HasStringPrefix(guildOptions.Prefix, ref argPos)) && !(msg.HasMentionPrefix(Client.CurrentUser, ref argPos))) return;

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
                    else if (Result.ErrorReason.ToLower().Contains("too many param"))
                    {
                        await Client_Log(new LogMessage(LogSeverity.Warning, "Client_MessageReceived", $"Invalid parameters sent by {context.Message.Author.ToString()} in guild: {context.Guild.Id} - Command text: {context.Message.Content}"));
                        var errorMsg = await context.Channel.SendMessageAsync($"Pretty sure you goofed on the parameters you've supplied there {context.Message.Author.Mention}!");
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
            await Client.SetGameAsync($"{bSettings.activity} {bSettings.version}");
        }
    }
}