using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Haphrain.Classes.Data
{
    internal class Poll
    {
        internal SocketUserMessage PollMessage { get; set; }
        internal List<string> PollOptions { get; set; } = new List<string>();
        internal string PollTitle { get; set; }
        internal List<PollReaction> PollReactions { get; set; } = new List<PollReaction>();

        internal Poll(SocketUserMessage message, string title, params string[] options)
        {
            PollMessage = message;
            PollTitle = title;
            PollOptions = options.ToList();
        }

        internal async Task<bool?> AddReaction(SocketUser usr, string vote)
        {
            if (!PollOptions.Contains(vote)) return null;
            if (PollReactions.Where(x => x.User.Id == usr.Id) == null) return false;
            PollReactions.Add(new PollReaction(usr, vote));
            await Update();
            return true;
        }
        internal async Task<bool?> RemoveReaction(SocketUser usr, string vote)
        {
            if (!PollOptions.Contains(vote)) return null;
            if (PollReactions.Where(x => x.User.Id == usr.Id) == null) return false;
            PollReactions.Remove(PollReactions.Single(x => x.User.Id == usr.Id && x.PollVote == vote));
            await Update();
            return true;
        }

        internal Embed CreatePollEmbed()
        {
            EmbedBuilder b = new EmbedBuilder();
            b.Title = PollTitle;
            float total = PollReactions.Count;
            foreach (string s in PollOptions)
            {
                float amt = PollReactions.Count(x => x.PollVote == s);
                b.AddField(s, $"{amt}/{total} ({(total / 100f) * amt}%)");
            }

            return b.Build();
        }

        private async Task Update()
        {
            await PollMessage.ModifyAsync(x => {
                x.Content = "";
                x.Embed = CreatePollEmbed();
            });
        }
    }

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
}
