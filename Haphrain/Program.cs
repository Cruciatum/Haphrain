using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Haphrain.Classes.HelperObjects;
using Haphrain.Classes.Data;
using System.Timers;
using System.Net.Http;

using System.Data.SqlClient;
using System.Collections.Generic;
using Haphrain.Classes.Commands;

namespace Haphrain
{
    class Program
    {
        private DiscordSocketClient Client;
        private CommandService Commands;
        private IServiceProvider Provider;
        private static readonly HttpClient httpClient = new HttpClient();
        private BotSettings bSettings;
        private DBSettings dbSettings;
        private Random r = new Random();

        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            var res = Setup.GetFiles(bSettings, dbSettings);
            res.Wait();
            bSettings = res.Result.botSettings;
            dbSettings = res.Result.dbSettings;

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

            if (!Directory.Exists(LogWriter.LogFileLoc.Replace($"Logs{Constants.slashType}Log", $"Logs{Constants.slashType}")))
            {
                Directory.CreateDirectory(LogWriter.LogFileLoc.Replace($"Logs{Constants.slashType}Log", $"Logs{Constants.slashType}"));
            }
            DBControl.dbSettings = dbSettings;

            await Client.LoginAsync(TokenType.Bot, bSettings.token);
            await Client.StartAsync();

            Timer t = new Timer();
            t.AutoReset = true;
            async void handler(object sender, ElapsedEventArgs e)
            {
                foreach (Poll p in GlobalVars.Polls)
                    await p.Update();
            }
            t.StartTimer(handler, 5000);

            await Task.Delay(-1);
        }

        private async Task GetSQLData()
        {
            //Load prefix & options from DB
            SqlConnectionStringBuilder sBuilder = new SqlConnectionStringBuilder();
            sBuilder.InitialCatalog = dbSettings.db;
            sBuilder.UserID = dbSettings.username;
            sBuilder.Password = dbSettings.password;
            sBuilder.DataSource = dbSettings.host + @"\" + dbSettings.instance + "," + dbSettings.port;
            sBuilder.ConnectTimeout = 30;
            sBuilder.IntegratedSecurity = false;
            SqlConnection conn = new SqlConnection();

            conn.ConnectionString = sBuilder.ConnectionString;
            
            using (conn)
            {
                conn.Open();

                #region Get Guilds
                SqlCommand cmd = new SqlCommand($"SELECT * FROM Guilds", conn);
                SqlDataReader dr = cmd.ExecuteReader();

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

                    if (!GlobalVars.GuildOptions.Any(x => x.GuildID == go.GuildID))
                        GlobalVars.GuildOptions.Add(go);
                }
                dr.Close();
                #endregion

                #region Get Friends
                cmd.CommandText = $"SELECT UserID FROM Friends";
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    var id = Convert.ToUInt64(dr.GetValue(0));
                    var user = await CustomUserTypereader.GetUserFromID(id, Client.Guilds);
                    if (!GlobalVars.FriendUsers.ContainsKey(id))
                        GlobalVars.FriendUsers.Add(id, user);
                }
                dr.Close();
                #endregion

                #region Get Idiots
                cmd.CommandText = $"SELECT UserID FROM Ignores";
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    var id = Convert.ToUInt64(dr.GetValue(0));
                    var user = await Classes.Commands.CustomUserTypereader.GetUserFromID(id, Client.Guilds);
                    if (!GlobalVars.IgnoredUsers.ContainsKey(id))
                        GlobalVars.IgnoredUsers.Add(id, user);
                }
                dr.Close();
                #endregion

                #region Get Emotes
                cmd.CommandText = $"SELECT * FROM Emotes";
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    ApprovedEmote ae = new ApprovedEmote(dr.GetValue(0).ToString(),dr.GetValue(3).ToString(),dr.GetValue(2).ToString(),Convert.ToBoolean(dr.GetValue(4)), dr.GetValue(5).ToString());
                    if (!GlobalVars.EmoteList.ContainsKey(ae.EmoteID))
                        GlobalVars.EmoteList.Add(ae.EmoteID, ae);
                }
                dr.Close();
                #endregion

                #region Get Bot Owners
                cmd.CommandText = $"SELECT * FROM BotOwners";
                dr = cmd.ExecuteReader();
                List<ulong> ownerList = new List<ulong>();
                while (dr.Read())
                {
                    ownerList.Add(Convert.ToUInt64(dr.GetValue(1)));
                }
                Constants._BOTOWNERS_ = ownerList.ToArray();
                dr.Close();
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
                            DBControl.UpdateDB($"UPDATE Guilds SET LogEmbeds = {(guildOptions.LogEmbeds?1:0)} WHERE GuildID = {guildID};");
                            if (!guildOptions.LogEmbeds)
                            {
                                await channel.SendMessageAsync("Now logging messages with embeds.");
                            }
                            else await channel.SendMessageAsync("No longer logging messages with embeds.");
                        }
                        else if (reaction.Emote.Name == "\u0032\u20E3")
                        {
                            guildOptions.LogAttachments = !guildOptions.LogAttachments;
                            DBControl.UpdateDB($"UPDATE Guilds SET LogAttachments = {(guildOptions.LogAttachments?1:0)} WHERE GuildID = {guildID};");
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
                if (GlobalVars.GuildOptions.SingleOrDefault(x => x.GuildID == g.Id) == null)
                {
                    await Client_JoinedGuild(g);
                }
            }
            foreach (GuildOption go in GlobalVars.GuildOptions)
            {
                if (Client.Guilds.SingleOrDefault(x => x.Id == go.GuildID) == null)
                {
                    DBControl.UpdateDB($"DELETE FROM Guilds WHERE GuildID = {go.GuildID};");
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
            o.LogEmbeds = false;
            o.LogAttachments = false;

            go.Options = o;
            if (!GlobalVars.GuildOptions.Any(x => x.GuildID == go.GuildID))
                GlobalVars.GuildOptions.Add(go);

            DBControl.UpdateDB($"INSERT INTO Guilds (GuildID,GuildName,OwnerID,Prefix,LogChannelID,LogEmbeds,LogAttachments) VALUES ({go.GuildID.ToString()}, '{go.GuildName.Replace(@"'","")}',{go.OwnerID.ToString()},'{go.Prefix}',{go.Options.LogChannelID.ToString()}, {(go.Options.LogEmbeds ? 1:0)}, {(go.Options.LogAttachments ? 1:0)});");

            await UpdateActivity();
            await Task.Delay(100);
        }

        private async Task Client_Log(LogMessage arg)
        {
            if (arg.Severity <= LogSeverity.Info)
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
            await GetSQLData();
            await UpdateActivity();
            await CheckGuildsStartup();
            await Client_Log(new LogMessage(LogSeverity.Info, "Client_Ready", "Bot ready!"));
        }

        private async Task Client_MessageReceived(SocketMessage arg)
        {
            var msg = arg as SocketUserMessage;
            var context = new SocketCommandContext(Client, msg);

            if (msg.Content.Length <= 1 && msg.Embeds.Count == 0 && msg.Attachments.Count == 0) return;
            if (context.User.IsBot) return;

            if (msg.Content.Length >= 2)
            {
                if (msg.Content.ToLower().Substring(0, 2) == "hi" && msg.Author.Id == 489410442029039657 && r.Next(0,5) == 2) { await context.Channel.SendMessageAsync($"Well hey there beautiful {msg.Author.Mention}"); return; }
            }

            var guildOptions = GlobalVars.GuildOptions.Single(x => x.GuildID == context.Guild.Id);

            if ((context.Message == null || context.Message.Content == "") && arg.Attachments.Count == 0 && arg.Embeds.Count == 0) return;
            
            if (GlobalVars.IgnoredUsers.ContainsKey(context.Message.Author.Id)) return;

            int argPos = 0;

            if (guildOptions.Options.LogChannelID != 0)
            {
                if (guildOptions.Options.LogEmbeds)
                    if (msg.Embeds.Count > 0) { await ImageLogger.LogEmbed(msg, guildOptions.Options.LogChannelID, Client); }
                if (guildOptions.Options.LogAttachments)
                    if (msg.Attachments.Count > 0) { await ImageLogger.LogAttachment(msg, guildOptions.Options.LogChannelID, Client); }
            }

            if (!(msg.HasStringPrefix(guildOptions.Prefix, ref argPos, StringComparison.CurrentCultureIgnoreCase)) && !(msg.HasMentionPrefix(Client.CurrentUser, ref argPos))) return;


            if (!await GlobalVars.CheckUserTimeout(context.Message.Author, context.Guild.Id, context.Channel)) return;
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
                var x = GlobalVars.UserTimeouts.SingleOrDefault(b => b.TrackedUser.Id == context.Message.Author.Id);
                if (x == null) GlobalVars.AddUserTimeout(context.Message.Author, context.Guild.Id);
            }
            catch (Exception ex)
            {
                await Client_Log(new LogMessage(LogSeverity.Critical, context.Message.Content, Result.ErrorReason, ex));
            }
        }

        private async Task UpdateActivity()
        {
            await Client.SetGameAsync($"{bSettings.activity.Replace("{count}", Client.Guilds.Count.ToString())} {bSettings.version}");
        }
    }
}