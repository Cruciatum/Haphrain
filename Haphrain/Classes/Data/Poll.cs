using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

using Newtonsoft.Json;

using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Haphrain.Classes.Data
{
    [Serializable]
    internal class Poll
    {
        internal SocketUser PollCreator { get; set; }
        internal RestUserMessage PollMessage { get; set; }
        internal List<PollOption> PollOptions { get; set; } = new List<PollOption>();
        internal string PollTitle { get; set; }
        internal List<PollReaction> PollReactions { get; set; } = new List<PollReaction>();
        internal uint PollId { get; set; }

        internal Poll(RestUserMessage message, string title, SocketUser usr, params string[] options)
        {
            PollCreator = usr;
            PollMessage = message;
            PollTitle = title;
            PollId = GetNextId();
            for (int i = 1; i <= options.Length; i++)
            {
                Emoji e = new Emoji($"{i.ToString()}\u20E3");
                PollOptions.Add(new PollOption(options[i - 1], e));
            }
        }

        internal async Task AddAllReactions()
        {
            List<Emoji> emojis = new List<Emoji>();
            foreach (PollOption po in PollOptions)
            {
                emojis.Add(po.React);
            }
            await PollMessage.AddReactionsAsync(emojis.ToArray());
        }

        internal bool? AddReaction(SocketUser usr, string vote)
        {
            if (PollOptions.Where(x=>x.Option == vote) == null) return null;
            if (PollReactions.Where(x => x.User.Id == usr.Id) == null) return false;
            PollReactions.Add(new PollReaction(usr, vote));
            return true;
        }
        internal bool? RemoveReaction(SocketUser usr, string vote)
        {
            if (PollOptions.Where(x=>x.Option == vote)==null) return null;
            if (PollReactions.Where(x => x.User.Id == usr.Id) == null) return false;
            PollReactions.Remove(PollReactions.Single(x => x.User.Id == usr.Id && x.PollVote == vote));
            return true;
        }

        internal Embed CreatePollEmbed()
        {
            EmbedBuilder b = new EmbedBuilder();
            b.WithAuthor($"Poll by {PollCreator.Username}#{PollCreator.DiscriminatorValue}", PollCreator.GetAvatarUrl());
            b.WithDescription($"**{PollTitle}**");
            b.WithFooter($"PollID: {PollId}");
            b.WithColor(0, 255, 0);
            float total = PollReactions.Count;
            foreach (PollOption s in PollOptions)
            {
                float amt = PollReactions.Count(x => x.PollVote == s.Option);
                if (total == 0)
                    b.AddField($"{s.React} {s.Option}", $"{GetPercentageBar(this, s.Option)} - {amt}/{total} (0,00%)");
                else
                    b.AddField($"{s.React} {s.Option}", $"{GetPercentageBar(this, s.Option)} - {amt}/{total} ({(amt / total * 100).ToString("N2")}%)");

            }

            return b.Build();
        }

        internal async Task Update()
        {
            try
            {
                await PollMessage.ModifyAsync(x =>
                {
                    x.Content = "";
                    x.Embed = CreatePollEmbed();
                });
            }
            catch (Discord.Net.HttpException e)
            {
                Console.WriteLine($"EXCEPTION: {(e.DiscordCode.HasValue ? e.DiscordCode : null)} - [{e.HResult}] {e.Reason}");
                await LogWriter.WriteLogFile($"EXCEPTION: {(e.DiscordCode.HasValue ? e.DiscordCode : null)} - [{e.HResult}] {e.Reason}");
                Console.WriteLine($"Request: {JsonConvert.SerializeObject(e.Request.Options)}");
                await LogWriter.WriteLogFile($"Request: {JsonConvert.SerializeObject(e.Request.Options)}");
                Console.WriteLine($"{e.StackTrace}");
                await LogWriter.WriteLogFile($"{e.StackTrace}");
            }
        }

        private static readonly Random r = new Random();
        private static uint _lastID_ = 1;
        private static uint GetNextId()
        {
            return _lastID_++;
        }
        public static string GetPercentageBar(Poll p, string s)
        {
            string bar = "[";
            float total = p.PollReactions.Count;
            float percentage = (p.PollReactions.Count(x => x.PollVote == s) / total) * 100;
            int i = 0;
            while (i < percentage)
            {
                bar += "\u25A0";
                i += 5;
            }
            while (i < 100)
            {
                bar += "\u25A1";
                i += 5;
            }
            bar += "]";
            return bar;
        }
    }

    [Serializable]
    internal class PollReaction
    {
        internal string PollVote { get; set; }
        internal SocketUser User { get; set; }

        internal PollReaction(SocketUser usr, string vote)
        {
            PollVote = vote;
            User = usr;
        }
    }
    [Serializable]
    internal class PollOption
    {
        internal string Option { get; set; }
        internal Emoji React { get; set; }

        public PollOption(string s, Emoji e)
        {
            Option = s;
            React = e;
        }
    }
}
