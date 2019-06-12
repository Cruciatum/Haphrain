using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Haphrain.Classes.Commands
{
    public class ModCommands : ModuleBase<SocketCommandContext>
    {
        [Command("setprefix"), Alias("prefix", "newprefix"), Summary("Set a new prefix for this server")]
        public async Task SetPrefix(string newPrefix)
        {
            SocketGuildUser guildUser = Context.Guild.Users.Single(u => u.Id == Context.User.Id);
            if (!guildUser.GuildPermissions.Administrator)
            {
                var uMsg = await Context.Channel.SendMessageAsync($"{Context.User.Mention}, this command requires you to have Administrator permissions.");
                var owner = Context.Guild.Owner;
                var channel = await owner.GetOrCreateDMChannelAsync();
                await channel.SendMessageAsync($"Hi {owner.Nickname}, user {Context.User.Mention} has tried to change my prefix in {Context.Guild.Name} in channel: #{Context.Channel.Name}");
                Timer t = new Timer();
                async void handler(object sender, ElapsedEventArgs e)
                {
                    t.Stop();
                    await uMsg.DeleteAsync();
                    await Context.Message.DeleteAsync();
                }
                t.StartTimer(handler, 10000);
            }
            else
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
        }

        [Command("goodbye"), Summary("Leave a server")]
        public async Task LeaveGuild()
        {
            if (Context.User.Id != Context.Guild.Owner.Id)
            {
                var uMsg = await Context.Channel.SendMessageAsync($"{Context.User.Mention}, NO, screw you! Only {Context.Guild.Owner.Mention} can make me leave!");

                var owner = Context.Guild.Owner;
                var channel = await owner.GetOrCreateDMChannelAsync();
                var msgToOwner = await channel.SendMessageAsync($"Hi {Context.Guild.Owner.Username}, user {Context.User.Mention} has tried to make me leave {Context.Guild.Name} in channel: #{Context.Channel.Name}");

                Timer t = new Timer();
                async void handler(object sender, ElapsedEventArgs e)
                {
                    t.Stop();
                    await uMsg.DeleteAsync();
                    await Context.Message.DeleteAsync();
                }
                t = t.StartTimer(handler, 10000);
            }
            else
            {
                await Context.Channel.SendMessageAsync($"Goodbye {Context.Guild.Owner.Mention}, apparantly {Context.User.Mention} wants me gone :sob:");
                await Context.Guild.LeaveAsync();
            }
        }

        [Command("setup"), Summary("Show settings for this server"), Priority(0)]
        public async Task ServerSettings()
        {
            EmbedBuilder builder = new EmbedBuilder();
            //Create an embed with all the listed settings
            //Add reactions in order to toggle specific settings on/off
        }
    }
}
