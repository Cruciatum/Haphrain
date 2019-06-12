using Discord.Commands;
using System;
using System.Threading.Tasks;
using System.Timers;
using System.Text.RegularExpressions;
using Discord.WebSocket;
using Discord;
using System.Collections.Generic;
using System.Linq;

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
                catch { /*No mention was found*/ }
            }

            time = time.ToLower();
            if (Regex.Match(time, @"^\d+[dhms]$").Success)
            {
                int i = time.Contains('d') ? time.IndexOf('d') : time.Contains('h') ? time.IndexOf('h') : time.Contains('m') ? time.IndexOf('m') : time.IndexOf('s');
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
                            string[] splitString = new string[2];
                            splitString = fullMessage.Split(mentionString);
                            await Context.Channel.SendMessageAsync(
                                string.Format("{0}, In {1} {2} I will remind you of your message. {3} {4} {5}",
                                Context.User.Mention,
                                (t / multiplier).ToString(),
                                code,
                                splitString[0] == "" ? "" : string.Concat("`", splitString[0], "`"),
                                user.Mention,
                                splitString[1] == "" ? "" : string.Concat("`", splitString[1], "`")));
                            await TimerStart(t * multiplier, Context.Channel, Context.User, fullMessage, user, mentionString);
                        }
                        else
                        {
                            await Context.Channel.SendMessageAsync($"{Context.User.Mention}, In {(t / multiplier).ToString()} {code} I will remind you of your message. `{fullMessage}`");
                            await TimerStart(t * multiplier, Context.Channel, Context.User, fullMessage);
                        }

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

        private async Task TimerStart(ulong time, ISocketMessageChannel channel, SocketUser user, string msg, IUser u = null, string mentionString = "")
        {
            Timer t = new Timer();
            async void handler(object sender, ElapsedEventArgs e)
            {
                t.Stop();
                if (u != null && mentionString != "")
                {
                    string[] splitString = new string[2];
                    splitString = msg.Split(mentionString);
                    await channel.SendMessageAsync(
                        string.Format("{0}, Automated reminder: {1} {2} {3}",
                        user.Mention,
                        splitString[0] == "" ? "" : string.Concat("`", splitString[0], "`"),
                        u.Mention,
                        splitString[1] == "" ? "" : string.Concat("`", splitString[1], "`")));
                }
                else
                {
                    await channel.SendMessageAsync($"{user.Mention}, Automated reminder: `{msg}`");
                }
            }
            t.Interval = time * 1000;
            t.Elapsed += handler;
            t.Start();
        }
    }

    public class CustomUserTypereader
    {
        public static async Task<IUser> GetUserFromString(string s, IGuild server)
        {
            if (s.IndexOf('@') == -1 || s.Replace("<", "").Replace(">", "").Length != s.Length - 2)
                throw new System.Exception("Not a valid user mention.");

            string idStr = s.Replace("<", "").Replace(">", "").Replace("@", "");

            try
            {
                ulong id = ulong.Parse(idStr);
                return await server.GetUserAsync(id);
            }
            catch
            {
                throw new Exception("Could not parse User ID.");
            }
        }
    }
}
