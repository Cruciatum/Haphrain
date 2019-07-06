using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Haphrain.Classes.HelperObjects;

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

        [Command("setup"), Summary("Show settings for this server"), Priority(0), RequireUserPermission(GuildPermission.Administrator)]
        public async Task ServerSettings()
        {
            Options guildOptions = new Options();
            GlobalVars.GuildsFile.Load(GlobalVars.GuildsFileLoc);
            var guildNode = GlobalVars.GuildsFile.SelectSingleNode($"/Guilds/Guild[@GuildID='{Context.Guild.Id}']");
            var prefixNode = guildNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "Prefix");
            var optionsNode = guildNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "Options");
            guildOptions.LogEmbeds = (optionsNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "LogEmbeds").InnerText == "0") ? false : true;
            guildOptions.LogAttachments = (optionsNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "LogAttachments").InnerText == "0") ? false : true;


            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "Server settings";
            builder.AddField("\u0031\u20E3 Log Embedded messages", $"Currently:{(guildOptions.LogEmbeds ? "True" : "False")}");
            builder.AddField("\u0032\u20E3 Log Attached files", $"Currently:{(guildOptions.LogAttachments ? "True" : "False")}");
        
            RestUserMessage msg = await Context.Channel.SendMessageAsync(null, false, builder.Build());
            GlobalVars.AddSettingsTracker(msg, Context.Message.Author.Id);
            await msg.AddReactionsAsync(new Emoji[] { new Emoji("\u0031\u20E3"), new Emoji("\u0032\u20E3") });
        }

        [Command("set"), Summary("View available settings"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task GetSets()
        {
            EmbedBuilder b = new EmbedBuilder();
            b.Title = "Available options:";
            b.AddField("set LogChannel", "Use in a channel to have logs appear in there");

            await Context.Channel.SendMessageAsync(null, false, b.Build());
        }

        [Command("set logchannel"), Alias("set lc"), Summary("Setup the channel for logging stuff"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetupLogChannel()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location).Replace(@"bin\Debug\netcoreapp2.1", @"Data\Guilds.xml"));
            var guildNode = doc.SelectSingleNode($"/Guilds/Guild[@GuildID='{Context.Guild.Id}']");
            var optionsNode = guildNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "Options");
            var channelNode = optionsNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "LogChannelID");
            RestUserMessage msg;
            if (channelNode.InnerText == "0")
            {
                msg = await Context.Channel.SendMessageAsync($"{Context.User.Mention}: Do you wish this channel to be used for logging?");
            }
            else
            {
                msg = await Context.Channel.SendMessageAsync($"{Context.User.Mention}: Do you wish to change the logging channel from {MentionUtils.MentionChannel(ulong.Parse(channelNode.InnerText))} to {MentionUtils.MentionChannel(Context.Channel.Id)}?");
            }
            GlobalVars.AddLogChannelTracker(msg, Context.Message.Author.Id);
            await msg.AddReactionsAsync(new Emoji[] { new Emoji("✅"), new Emoji("🚫") });
        }
    }
}