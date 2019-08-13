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
        [Command("setprefix"), Alias("prefix", "newprefix"), Summary("Set a new prefix for this server"), RequireUserPermission(GuildPermission.Administrator, ErrorMessage = "You require Administrator permissions to do this")]
        public async Task SetPrefix(string newPrefix)
        {
            if (newPrefix != null && newPrefix != "")
            {
                GlobalVars.GuildOptions.Single(x => x.GuildID == Context.Guild.Id).Prefix = newPrefix;
                DBControl.UpdateDB($"UPDATE Guilds SET Prefix = {newPrefix} WHERE GuildID = {Context.Guild.Id};");

                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, I have updated your server's prefix to {newPrefix}");
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

                GlobalVars.AddRandomTracker(uMsg);
            }
            else
            {
                await Context.Channel.SendMessageAsync($"Goodbye {Context.Guild.Owner.Mention}, apparantly {Context.User.Mention} wants me gone :sob:");
                await Context.Guild.LeaveAsync();
            }
        }

        [Command("setup"), Summary("Show settings for this server"), Priority(0), RequireUserPermission(GuildPermission.Administrator, ErrorMessage = "You require Administrator permissions to do this")]
        public async Task ServerSettings()
        {
            Options guildOptions = GlobalVars.GuildOptions.Single(x => x.GuildID == Context.Guild.Id).Options;


            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "Server settings";
            builder.AddField("\u0031\u20E3 Log Embedded messages", $"Currently:{(guildOptions.LogEmbeds ? "True" : "False")}");
            builder.AddField("\u0032\u20E3 Log Attached files", $"Currently:{(guildOptions.LogAttachments ? "True" : "False")}");

            RestUserMessage msg = await Context.Channel.SendMessageAsync(null, false, builder.Build());
            GlobalVars.AddSettingsTracker(msg, Context.Message.Author.Id);
            await msg.AddReactionsAsync(new Emoji[] { new Emoji("\u0031\u20E3"), new Emoji("\u0032\u20E3") });
        }

        [Command("set"), Summary("View available settings"), RequireUserPermission(GuildPermission.Administrator, ErrorMessage = "You require Administrator permissions to do this")]
        public async Task GetSets()
        {
            EmbedBuilder b = new EmbedBuilder();
            b.Title = "Available options:";
            b.AddField("set LogChannel", "Use in a channel to have logs appear in there");

            await Context.Channel.SendMessageAsync(null, false, b.Build());
        }

        [Command("set logchannel"), Alias("set lc"), Summary("Setup the channel for logging stuff"), RequireUserPermission(GuildPermission.Administrator, ErrorMessage = "You require Administrator permissions to do this")]
        public async Task SetupLogChannel()
        {
            Options guildOptions = GlobalVars.GuildOptions.Single(x => x.GuildID == Context.Guild.Id).Options;
            RestUserMessage msg;
            if (guildOptions.LogChannelID == 0)
            {
                msg = await Context.Channel.SendMessageAsync($"{Context.User.Mention}: Do you wish this channel to be used for logging?");
            }
            else
            {
                msg = await Context.Channel.SendMessageAsync($"{Context.User.Mention}: Do you wish to change the logging channel from {MentionUtils.MentionChannel(guildOptions.LogChannelID)} to {MentionUtils.MentionChannel(Context.Channel.Id)}?");
            }
            GlobalVars.AddLogChannelTracker(msg, Context.Message.Author.Id);
            await msg.AddReactionsAsync(new Emoji[] { new Emoji("✅"), new Emoji("🚫") });
        }
    }
}