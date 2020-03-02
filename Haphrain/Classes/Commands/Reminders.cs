using Discord.Commands;
using System;
using System.Threading.Tasks;
using System.Timers;
using System.Text.RegularExpressions;
using Discord.WebSocket;
using Discord;
using System.Collections.Generic;
using System.Linq;
using Haphrain.Classes.Data;
using Haphrain.Classes.HelperObjects;

namespace Haphrain.Classes.Commands
{
    public class Reminders : ModuleBase<SocketCommandContext>
    {
        [Command("remind"), Alias("rm", "r"), Summary("Set up a timed reminder")]
        public async Task RemindUser(string time, params string[] msg)
        {
            IUser user = null;
            List<IUser> userList = new List<IUser>();
            string mentionString = "";
            userList.AddRange(Context.Guild.Users);
            userList = userList.GroupBy(o => o.Id).Select(o => o.First()).ToList();
            bool hasMention = false;

            foreach (string s in msg)
            {
                try { user = await CustomUserTypereader.GetUserFromString(s, Context.Guild); mentionString = s; hasMention = true; }
                catch
                {
                    //No Mention found
                }
            }

            time = time.ToLower();
            if (Regex.Match(time, @"^\d+[dhms]$").Success)
            {
                int i = time.Contains('d') ? time.IndexOf('d') : (time.Contains('h') ? time.IndexOf('h') : (time.Contains('m') ? time.IndexOf('m') : time.IndexOf('s')));
                string code = "";
                ulong multiplier = 1;
                switch (time.Substring(i, 1))
                {
                    case "d":
                        code = "days"; multiplier = 24 * 60 * 60; break;
                    case "h":
                        code = "hours"; multiplier = 60 * 60; break;
                    case "m":
                        code = "minutes"; multiplier = 60; break;
                    default:
                        code = "seconds"; break;
                }
                ulong t = ulong.Parse(time.Substring(0, i)) * multiplier;
                if (t > (7 * 24 * 60 * 60)) { await Context.Channel.SendMessageAsync($"Timespan too large, max amount of time: 7 days *({7 * 24} hours/{7 * 24 * 60} minutes/{7 * 24 * 60 * 60}seconds)*"); return; }
                if (msg[0] != "")
                {
                    string fullMessage = string.Join(' ', msg);
                    if (fullMessage.Length > 100) { await Context.Channel.SendMessageAsync($"{Context.User.Mention}, your message is too long, max character limit: 100"); return; }
                    else
                    {
                        if (hasMention)
                        {
                            fullMessage.Replace(mentionString, user.Username);
                        }
                        await Context.Channel.SendMessageAsync($"{Context.User.Mention}, In {(t / multiplier).ToString()} {code} I will remind you of your message. `{fullMessage}`");
                        TimerStart(t*1000, Context.Channel, Context.User, fullMessage);

                        DateTime triggerAt = DateTime.Now.AddSeconds(t);

                        string sql = $"INSERT INTO Timers (UserID, GuildID, ChannelID, TimerType, TriggerTime, TimerMessage) VALUES (";
                        sql += $"{Context.User.Id}, ";
                        sql += $"{Context.Guild.Id}, ";
                        sql += $"{Context.Channel.Id}, 'remind', ";
                        sql += $"'{triggerAt.Year}-{triggerAt.Month}-{triggerAt.Day}-{triggerAt.Hour}-{triggerAt.Minute}-{triggerAt.Second}-{triggerAt.Millisecond}', ";
                        sql += $"'{Sanitize(fullMessage)}')";

                        DBControl.UpdateDB(sql);
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"{Context.User.Mention}, You forgot to add a message!");
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync($"Invalid time format");
            }
        }

        internal void TimerStart(ulong time, ISocketMessageChannel channel, SocketUser user, string msg)
        {
            Timer t = new Timer();
            async void handler(object sender, ElapsedEventArgs e)
            {
                t.Stop();
                await channel.SendMessageAsync($"{user.Mention}, Automated reminder: `{msg}`");
            }
            t.StartTimer(handler, time);
        }

        internal string Sanitize(string input)
        {
            string output = "";
            foreach (char c in input)
            {
                if (c.ToString() == "'")
                    output += "§²";
                else if (c.ToString() == ";")
                    output += "§³";
                else
                    output += c.ToString();
            }
            return output;
        }
    }

    public class CustomUserTypereader
    {
        public static async Task<IUser> GetUserFromString(string s, IGuild server)
        {
            if (s.IndexOf('@') == -1 || s.Replace("<", "").Replace(">", "").Length != s.Length - 2)
                throw new Exception("Not a valid user mention.");

            string idStr = s.Replace("<", "").Replace(">", "").Replace("@", "").Replace("!", "");

            try
            {
                ulong id = ulong.Parse(idStr);
                return await server.GetUserAsync(id);
            }
            catch (Exception ex)
            {
                await LogWriter.WriteLogFile($"ERROR: Exception thrown : {ex.Message}");
                await LogWriter.WriteLogFile($"{ex.StackTrace}");
                Console.WriteLine($"Exception: {ex.Message}");
                throw ex;
            }
        }
        public static async Task<IUser> GetUserFromID(ulong id, IReadOnlyCollection<IGuild> guilds)
        {
            IUser userFound = null;
            try
            {
                foreach (IGuild g in guilds)
                {
                    userFound = await g.GetUserAsync(id);
                    if (userFound != null) break;
                }
            }
            catch (Exception ex)
            {
                await LogWriter.WriteLogFile($"ERROR: Exception thrown : {ex.Message}");
                await LogWriter.WriteLogFile($"{ex.StackTrace}");
                Console.WriteLine($"Exception: {ex.Message}");
                throw ex;
            }

            return userFound;
        }
    }
}
