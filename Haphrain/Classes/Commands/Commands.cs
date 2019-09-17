using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haphrain.Classes.Commands
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        [Command("commands"), Alias("cmds","help", "h"), Summary("View a list of available commands")]
        public async Task Cmds(string subject = "")
        {
            var builder = new EmbedBuilder();
            string prefix = GlobalVars.GuildOptions.SingleOrDefault(x => x.GuildID == Context.Guild.Id).Prefix;

            switch (subject.ToLower())
            {
                case "misc":
                    builder.AddField($"{prefix}goodbye", "Make me leave this server\n(Server owner only)");
                    builder.AddField($"{prefix}coinflip", "Flip a coin!");
                    break;

                case "remind":
                    builder.AddField($"{prefix}remind <#><d/h/m/s> <message>", "Set up a timed reminder\n(Doesn't work if bot is restarted the reminder is sent)");
                    break;

                case "settings":
                    builder.AddField($"{prefix}setPrefix <NewPrefix>", "Change the prefix this bot should react to on your server\n(Administrators only)");
                    builder.AddField($"{prefix}setup", "Change what types of messages are to be logged\n(Administrators only)");
                    builder.AddField($"{prefix}set", "Show available server settings");
                    break;

                case "emotes":
                    builder.AddField($"{prefix}emote <trigger> [@user]", "Trigger a custom emote!");
                    builder.AddField($"{prefix}emote list", "Show a list of currently available emotes");
                    builder.AddField($"{prefix}emote request <trigger> <imageURL> <true/false> <OutputMessage>", "Request a custom emote!\n"
                        + "Insert true if you want this to be a targetted emote, false if you don't\n"
                        + "Specify where you want the person triggering the emote's name in <OutputMessage> by using {author}\n"
                        + "Specify where you want the (optional) target's name in <OutputMessage> by using {target}");
                    break;

                case "convert":
                    builder.AddField("Distance Conversion", $"{prefix}convert dist <#> <start unit> <end unit>\n[Supported units: km/m/cm/mm; mi/yd/ft/inch]");
                    builder.AddField("Temperature Conversion", $"{prefix}convert temp <#> <start unit> <end unit>\n[Supported units: C/F/K]");
                    builder.AddField("Liquid Measure Conversion", $"{prefix}convert liq <#> <start unit> <end unit>\n[Supported units: l/dl/cl/ml; gal/oz]");
                    break;

                case "define":
                    builder.AddField($"{prefix}define", "Perform a search on Oxford Dictionary for your term\nDefaults to Urban Dictionary if the term isn't found");
                    builder.AddField($"{prefix}define urb", "Perform a search on Urban Dictionary for your term");
                    break;

                case "poll":
                    builder.AddField($"{prefix}poll create <question> | <timecode> | <option> | <option> | [<option>] | [<option>] [<option>]", "Create a new poll in this channel\n**Timecode**: #(d/h)\n__MIN__ 2 options \n__MAX__ 5 options");
                    builder.AddField($"{prefix}poll close <id>", "Close a poll with the given ID");
                    builder.AddField($"{prefix}poll reset <id>", "Delete all votes from a poll & have me re-post it.");
                    break;

                default:
                    builder.WithTitle("Available categories");
                    builder.AddField("Polls", $"For more details: {prefix}help poll");
                    builder.AddField("Conversions", $"For more details: {prefix}help convert");
                    builder.AddField("Emotes", $"For more details: {prefix}help emotes");
                    builder.AddField("Reminders", $"For more details: {prefix}help remind");
                    builder.AddField("Definitions", $"For more details: {prefix}help define");
                    builder.AddField("Settings", $"For more details: {prefix}help settings");
                    builder.AddField("Misc", $"For more details: {prefix}help misc");
                    break;
            }
            
            await Context.Channel.SendMessageAsync(null, false, builder.Build());
        }
    }
}
