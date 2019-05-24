using Discord.Commands;
using System;
using System.Threading.Tasks;
using System.Timers;
using System.Text.RegularExpressions;
using Discord.WebSocket;

namespace Haphrain.Classes.Commands
{
    public class Reminders : ModuleBase<SocketCommandContext>
    {
        [Command("remind"), Alias("rm", "r"), Summary("Set up a timed reminder")]
        public async Task RemindUser(string time, params string[] msg)
        {
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
                    string fullMessage = String.Join(' ', msg);
                    if (fullMessage.Length > 100) { await Context.Channel.SendMessageAsync($"{Context.User.Mention}, your message is too long, max character limit: 100"); return; }
                    else
                    {
                        await Context.Channel.SendMessageAsync($"{Context.User.Mention}, In {(t / multiplier).ToString()} {code} I will remind you of your message. `{fullMessage}`");
                        await TimerStart(t * multiplier, Context.Channel, Context.User, fullMessage);
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

        private async Task TimerStart(ulong time, ISocketMessageChannel channel, SocketUser user, string msg)
        {
            Timer t = new Timer();
            ElapsedEventHandler handler =
                async delegate (object sender, ElapsedEventArgs e)
                {
                    t.Stop();
                    await channel.SendMessageAsync($"{user.Mention}, Automated reminder: `{msg}`");
                };
            t.Interval = time * 1000;
            t.Elapsed += handler;
            t.Start();
        }
    }
}
