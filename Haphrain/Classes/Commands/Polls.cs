using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Haphrain.Classes.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Haphrain.Classes.Commands
{
    public class Polls : ModuleBase<SocketCommandContext>
    {
        [Command("poll"), Summary("Show options related to polls")]
        public async Task PollSummary()
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithTitle("Available options:");
            eb.AddField("poll create <question> | <timecode> | <option> | <option> | [<option>] | [<option>] [<option>]", "Create a new poll in this channel\n**Timecode**: #(d/h)\n__MIN__ 2 options \n__MAX__ 5 options");
            eb.AddField("poll close <id>", "Close a poll with the given ID");
            eb.AddField("poll reset <id>", "Delete all votes from a poll & have me re-post it.");

            await Context.Channel.SendMessageAsync(null, false, eb.Build());
        }

        [Command("poll create"), Summary("Show options related to polls")]
        public async Task PollCreate([Remainder]string parameters)
        {
            string[] parameterArray = parameters.Split('|');
            if (parameterArray.Length >= 4 && parameterArray.Length <= 7)
            {
                string question = parameterArray[0];
                parameterArray[0] = "";
                string timecode = parameterArray[1];
                parameterArray[1] = "";

                ulong timeSpan = 0;
                timecode = timecode.ToLower();
                if (Regex.Match(timecode, @"\d+[dh]").Success)
                {
                    int i = timecode.Contains('d') ? timecode.IndexOf('d') : (timecode.IndexOf('h'));
                    ulong multiplier = 1;
                    switch (timecode.Substring(i, 1))
                    {
                        case "d":
                            multiplier = 24 * 60 * 60; break;
                        default:
                            multiplier = 60 * 60; break;
                    }
                    timeSpan = ulong.Parse(timecode.Substring(0, i)) * multiplier;
                    if (timeSpan > (7 * 24 * 60 * 60)) { await Context.Channel.SendMessageAsync($"Timespan too large, max amount of time: 7 days *({7 * 24} hours)*"); return; }
                }

                List<string> pollOptions = new List<string>();
                foreach (string s in parameterArray)
                {
                    if (s != "")
                    {
                        pollOptions.Add(s);
                    }
                }

                var m = await Context.Channel.SendMessageAsync("Poll creating...");
                Poll p = new Poll(m, question, Context.User, pollOptions.ToArray());

                EmbedBuilder eb = new EmbedBuilder();
                eb.WithAuthor($"Poll by {Context.User.Username}#{Context.User.DiscriminatorValue}", Context.User.GetAvatarUrl());
                eb.WithDescription($"**{question}**");
                eb.WithFooter($"PollID: {p.PollId}");
                eb.WithColor(0, 255, 0);
                foreach (PollOption s in p.PollOptions)
                {
                    float amt = p.PollReactions.Count(x => x.PollVote == s.Option);
                    eb.AddField($"{s.React} {s.Option}", $"{Poll.GetPercentageBar(p, s.Option)} - 0/{p.PollReactions.Count} (0,00%)");
                }

                await m.ModifyAsync(x => {
                    x.Content = "";
                    x.Embed = eb.Build();
                });
                await p.AddAllReactions();
                GlobalVars.AddPoll(p, timeSpan);
            }
            else
            {
                var m =  await Context.Channel.SendMessageAsync("Too many parameters");
                GlobalVars.AddRandomTracker(m);
            }
        }

        [Command("poll close"), Summary("Close by ID")]
        public async Task PollClose(uint id)
        {
            Poll p = GlobalVars.Polls.SingleOrDefault(x => x.PollId == id);

            EmbedBuilder eb = new EmbedBuilder();
            eb.WithAuthor($"CLOSED | Poll by {p.PollCreator.Username}#{p.PollCreator.DiscriminatorValue}", p.PollCreator.GetAvatarUrl());
            eb.WithDescription($"~~**{p.PollTitle}**~~");
            eb.WithFooter($"PollID: {p.PollId}");
            foreach (PollOption s in p.PollOptions)
            {
                float amt = p.PollReactions.Count(x => x.PollVote == s.Option);
                eb.AddField($"{s.React} {s.Option}", $"{Poll.GetPercentageBar(p, s.Option)} - {amt}/{p.PollReactions.Count} ({(amt / p.PollReactions.Count * 100).ToString("N2")}%)");
            }
            eb.WithColor(255, 0, 0);

            await p.PollMessage.ModifyAsync(x => {
                x.Content = "";
                x.Embed = eb.Build();
            });
            await p.PollMessage.Channel.SendMessageAsync("Poll closed", false, eb.Build());
            await p.PollMessage.DeleteAsync();

            GlobalVars.Polls.Remove(p);
        }

        [Command("poll reset"), Summary("Reset poll by ID")]
        public async Task PollReset(uint id)
        {
            Poll p = GlobalVars.Polls.SingleOrDefault(x => x.PollId == id);
            p.PollReactions = new List<PollReaction>();
            await p.PollMessage.DeleteAsync();
            p.PollMessage = await Context.Channel.SendMessageAsync("Re-posting poll");
            await p.AddAllReactions();
            await p.Update();
        }
    }
}
