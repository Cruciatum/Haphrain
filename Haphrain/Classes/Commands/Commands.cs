using Discord;
using Discord.Commands;
using Discord.WebSocket;
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
            var target = Context.Channel;

            switch (subject.ToLower())
            {
                case "misc":
                    builder.WithTitle("Help: Miscellaneous");
                    builder.AddField($"{prefix}goodbye", "Make me leave this server\n(Server owner only)");
                    builder.AddField($"{prefix}coinflip", "Flip a coin!");
                    builder.AddField($"{prefix}roll", "Roll a number of dice of whatever value you specify!\n" +
                        $"Format: *{prefix}roll <amount>D<size>[+/-][<modifier>]*");
                    break;

                case "remind":
                    builder.WithTitle("Help: Remind");
                    builder.AddField($"{prefix}remind <#><d/h/m/s> <message>", "Set up a timed reminder\n(Doesn't work if bot is restarted the reminder is sent)");
                    break;

                case "settings":
                    builder.WithTitle("Help: Settings");
                    builder.AddField($"{prefix}setPrefix <NewPrefix>", "Change the prefix this bot should react to on your server\n(Administrators only)");
                    builder.AddField($"{prefix}setup", "Change what types of messages are to be logged\n(Administrators only)");
                    builder.AddField($"{prefix}set", "Show available server settings");
                    break;

                case "emotes":
                    builder.WithTitle("Help: Emotes");
                    builder.AddField($"{prefix}emote <trigger> [@user]", "Trigger a custom emote!");
                    builder.AddField($"{prefix}emote list", "Show a list of currently available emotes");
                    builder.AddField($"{prefix}emote request <trigger> <imageURL> <true/false> <OutputMessage>", "Request a custom emote!\n"
                        + $"Use {prefix}help request for more help and some examples.");
                    break;

                case "request":
                    builder.WithTitle("Help: Requests");
                    var dmTarget = await Context.Message.Author.GetOrCreateDMChannelAsync();
                    builder.AddField($"{prefix}emote request <trigger> <imageURL> <true/false> <OutputMessage>",
                        "**<trigger>**: The word through which users call the emote.\n" +
                        "**<imageURL>**: A link to a .jpg, .gif or .png image.\n" +
                        "**<true/false>**: Specify whether or not another user is to be mentioned.\n" +
                        "**<OutputMessage>**: What should the bot say when this emote is used?\n" +
                        "Use `{author}` and `{target}` to specify where people should be mentioned");
                    builder.AddField($"Example 1 (with target):", $"{prefix}emote request **slap `link to image` true {{author}} slaps {{target}} across the face!**");
                    builder.AddField($"Example 2 (without target):", $"{prefix}emote request **angry `link to image` false {{author}} is furious!**");
                    while (dmTarget == null) { }
                    await dmTarget.SendMessageAsync(null, false, builder.Build());
                    return;

                case "convert":
                    builder.WithTitle("Help: Convert");
                    builder.AddField("Distance Conversion", $"{prefix}convert dist <#> <start unit> <end unit>\n[Supported units: km/m/cm/mm; mi/yd/ft/inch]");
                    builder.AddField("Temperature Conversion", $"{prefix}convert temp <#> <start unit> <end unit>\n[Supported units: C/F/K]");
                    builder.AddField("Liquid Measure Conversion", $"{prefix}convert liq <#> <start unit> <end unit>\n[Supported units: l/dl/cl/ml; gal/oz]");
                    builder.AddField("Weight Conversion", $"{prefix}convert wgt <#> <start unit> <end unit>\n[Supported units: kg/g/dg/cg/mg; st/lbs/oz]");
                    break;

                case "define":
                    builder.WithTitle("Help: Define");
                    builder.AddField($"{prefix}define", "Perform a search on Oxford Dictionary for your term\nDefaults to Urban Dictionary if the term isn't found");
                    builder.AddField($"{prefix}define urb", "Perform a search on Urban Dictionary for your term");
                    break;

                case "poll":
                    builder.WithTitle("Help: Polls");
                    builder.AddField($"{prefix}poll create <question> | <timecode> | <option> | <option> | [<option>] | [<option>] [<option>]", "Create a new poll in this channel\n**Timecode**: #(d/h)\n__MIN__ 2 options \n__MAX__ 5 options");
                    builder.AddField($"{prefix}poll close <id>", "Close a poll with the given ID");
                    builder.AddField($"{prefix}poll reset <id>", "Delete all votes from a poll & have me re-post it.");
                    break;

                case "morty":
                    builder.WithTitle("Help: Pocket Mortys");
                    builder.AddField($"{prefix}mortystart", "Register yourself as a Pocket Morty player!");
                    builder.AddField($"{prefix}morty","Attempt to add another Morty to your collection!");
                    builder.AddField($"{prefix}mortyinfo <ID/Name>", "View base stats and info about a specific Morty.");
                    builder.AddField($"{prefix}mortylist [Page #]", "View which Pocket Mortys you own so far!");
                    builder.AddField($"{prefix}mortyorder <order option> <asc/desc>", "Select how you want your Mortys to be ordered!\n*Available order options: `id, rarity, stattotal, count`*");
                    break;

                default:
                    builder.WithTitle("Available categories");
                    builder.AddField("Pocket Mortys", $"For more details: {prefix}help morty");
                    builder.AddField("Polls", $"For more details: {prefix}help poll");
                    builder.AddField("Conversions", $"For more details: {prefix}help convert");
                    builder.AddField("Emotes", $"For more details: {prefix}help emotes");
                    builder.AddField("Reminders", $"For more details: {prefix}help remind");
                    builder.AddField("Definitions", $"For more details: {prefix}help define");
                    builder.AddField("Settings", $"For more details: {prefix}help settings");
                    builder.AddField("Misc", $"For more details: {prefix}help misc");
                    break;
            }
            
            await target.SendMessageAsync(null, false, builder.Build());
        }
    }
}
