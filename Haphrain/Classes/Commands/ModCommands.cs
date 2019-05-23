using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using Discord;
using Discord.Commands;

namespace Haphrain.Classes.Commands
{
    public class ModCommands : ModuleBase<SocketCommandContext>
    {
        [Command("setprefix"), Alias("prefix", "newprefix"), Summary("Set a new prefix for this server"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetPrefix(string newPrefix)
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
                //Checking something
            }
        }

        [Command("goodbye"), Summary("Leave a server"), RequireOwner]
        public async Task LeaveGuild()
        {
            await Context.Channel.SendMessageAsync($"Goodbye {Context.Guild.Owner.Mention}, apparantly {Context.User.Mention} wants me gone :sob:");
            await Context.Guild.LeaveAsync();
        }
    }
}
