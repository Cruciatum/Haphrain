using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Haphrain.Classes.Commands
{
    public class ModCommands : ModuleBase<SocketCommandContext>
    {
        [Command("coinflip"), Alias("cf", "coin", "flip"), Summary("Flip a coin")]
        public async Task coinflip()
        {
            int result = 0;
            Random r = new Random();
            result = r.Next(0, 100);

            Console.WriteLine($"{DateTime.Now} -> Executed coin flip, result: {result}");
            await Context.Channel.SendMessageAsync($"{Context.User.Mention} your coin landed on {(result % 2 == 0 ? "Heads" : "Tails")}");
        }

        [Command("setprefix"), Alias("prefix", "newprefix"), Summary("Set a new prefix for this server"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task setPrefix(string newPrefix)
        {
            if (newPrefix != null && newPrefix != "")
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location).Replace(@"bin\Debug\netcoreapp2.1", @"Data\Guilds.xml"));

                var guildNode = doc.SelectSingleNode($"/Guilds/Guild[@GuildID='{Context.Guild.Id}']");
                var prefixNode = guildNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "Prefix");
                prefixNode.InnerText = newPrefix;

                doc.Save(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location).Replace(@"bin\Debug\netcoreapp2.1", @"Data\Guilds.xml"));

                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, I have updated your server's prefix to {newPrefix}");
            }
        }

        [Command("goodbye"), Summary("Leave a server"), RequireOwner]
        public async Task leaveGuild()
        {
            await Context.Channel.SendMessageAsync($"Goodbye {Context.Guild.Owner.Mention}, apparantly {Context.User.Mention} wants me gone :sob:");
            await Context.Guild.LeaveAsync();
        }
    }
}
