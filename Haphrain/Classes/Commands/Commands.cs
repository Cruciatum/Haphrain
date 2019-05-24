using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Haphrain.Classes.Commands
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        [Command("commands"), Alias("cmds"), Summary("View a list of available commands")]
        public async Task Cmds()
        {
            var builder = new EmbedBuilder();
            builder.Title = "Commands";
            builder.AddField("Coinflip (cf,coin,flip)", "Flip a coin to get heads or tails");
            builder.AddField("SetPrefix (Prefix, NewPrefix)", "Select a new prefix to be used on this server (Administrators only)\nUsage: `{prefix}SetPrefix {NewPrefix}`");
            builder.AddField("Remind", "Set up a timed reminder\nUsage: `{prefix}Remind {amount}{d/h/m/s} {message}s");
            builder.AddField("Goodbye", "Make the bot leave the server (Owner only)");

            await Context.Channel.SendMessageAsync(null, false, builder.Build());
        }
    }
}
