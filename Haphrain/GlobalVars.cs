using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Haphrain.Classes.Commands;
using Haphrain.Classes.HelperObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using Haphrain.Classes.Data;

namespace Haphrain
{
    internal static class GlobalVars
    {
        internal static DiscordSocketClient Client { get; set; }

        internal readonly static string GuildsFileLoc = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location).Replace(@"bin\Debug\netcoreapp2.1", @"Data\Guilds.xml");
        internal static XmlDocument GuildsFile { get; set; } = new XmlDocument();

        internal static List<TrackedMessage> TrackedLogChannelMessages { get; set; } = new List<TrackedMessage>(); //For changing log channel
        internal static List<TrackedMessage> TrackedSettingsMessages { get; set; } = new List<TrackedMessage>(); //For changing settings
        internal static List<TrackedMessage> RandomMessages { get; set; } = new List<TrackedMessage>(); //For Random other stuff including error messages

        internal static void AddLogChannelTracker(RestUserMessage msg, ulong authorID)
        {
            var tMsg = new TrackedMessage(msg, authorID);
            Timer t = new Timer();
            async void handler(object sender, ElapsedEventArgs e)
            {
                t.Stop();
                await UntrackMessage(tMsg);
            }
            t.StartTimer(handler, 60000);
            TrackedLogChannelMessages.Add(tMsg);
        }
        internal static void AddSettingsTracker(RestUserMessage msg, ulong authorID)
        {
            var tMsg = new TrackedMessage(msg, authorID);
            Timer t = new Timer();
            async void handler(object sender, ElapsedEventArgs e)
            {
                t.Stop();
                await UntrackMessage(tMsg);
            }
            t.StartTimer(handler, 60000);
            TrackedSettingsMessages.Add(tMsg);
        }
        internal static void AddRandomTracker(RestUserMessage msg)
        {
            var tMsg = new TrackedMessage(msg, 0);
            Timer t = new Timer();
            async void handler(object sender, ElapsedEventArgs e)
            {
                t.Stop();
                await UntrackMessage(tMsg);
            }
            t.StartTimer(handler, 15000);
            RandomMessages.Add(tMsg);
        }

        internal static List<TimeoutTracker> UserTimeouts { get; set; } = new List<TimeoutTracker>(); //Keep track of user timeouts after command usage
        internal static List<TimeoutTimer> UserTimeoutTimers { get; set; } = new List<TimeoutTimer>(); //Keep track of timers so proper one can be found

        internal static void AddUserTimeout(SocketUser usr, ulong guildID)
        {
            var track = new TimeoutTracker(usr, guildID);
            TimeoutTimer tTimer = null;
            Timer t = new Timer();
            void handler(object sender, ElapsedEventArgs e)
            {
                t.Stop();
                if (UserTimeouts.Contains(track))
                {
                    UserTimeouts.Remove(track);
                    UserTimeoutTimers.Remove(tTimer);
                }
            }
            t.StartTimer(handler, (int)(Constants._CMDTIMEOUT_ * 1000));
            tTimer = new TimeoutTimer(track);
            UserTimeouts.Add(track);
            UserTimeoutTimers.Add(tTimer);
        }
        internal static async Task<bool> CheckUserTimeout(SocketUser usr, ulong guildID, IMessageChannel channel)
        {
            var Tracker = UserTimeouts.SingleOrDefault(ut => ut.TrackedUser == usr && ut.GuildID == guildID);
            if (Tracker != null)
            {
                TimeoutTimer t = UserTimeoutTimers.SingleOrDefault(p => p.Tracker == Tracker);
                var msg = await channel.SendMessageAsync($"Slow down {usr.Username}! Try again in {TimeSpan.FromSeconds((int)Constants._CMDTIMEOUT_ - (DateTime.Now - t.StartTime).TotalSeconds).Seconds}.{(TimeSpan.FromSeconds(5 - (DateTime.Now - t.StartTime).TotalSeconds).Milliseconds) / 100}seconds.");
                AddRandomTracker((RestUserMessage)msg);
                return false;
            }
            return true;
        }

        internal static List<Poll> Polls { get; set; } = new List<Poll>();
        internal static void AddPoll(Poll p, ulong duration)
        {
            Timer t = new Timer();
            async void handler(object sender, ElapsedEventArgs e)
            {
                t.Stop();
                EmbedBuilder eb = new EmbedBuilder();
                eb.WithAuthor($"CLOSED | Poll by {p.PollCreator.Username}#{p.PollCreator.DiscriminatorValue}", p.PollCreator.GetAvatarUrl());
                eb.WithDescription($"~~**{p.PollTitle}**~~");
                eb.WithFooter($"PollID: {p.PollId}");
                foreach (PollOption s in p.PollOptions)
                {
                    float amt = p.PollReactions.Count(x => x.PollVote == s.Option);
                    eb.AddField($"{s.React} {s.Option}", $"{Poll.GetPercentageBar(p, s.Option)} - 0/{p.PollReactions.Count} (0%)");
                }
                eb.WithColor(255, 0, 0);

                await p.PollMessage.ModifyAsync(x => {
                    x.Content = "";
                    x.Embed = eb.Build();
                });
                if (Polls.Contains(p)) Polls.Remove(p);
            }
            t.StartTimer(handler, duration*1000);
            Polls.Add(p);
        }

        internal static async Task UntrackMessage(TrackedMessage msg)
        {
            if (TrackedLogChannelMessages.Contains(msg)) TrackedLogChannelMessages.Remove(msg);
            else if (TrackedSettingsMessages.Contains(msg)) TrackedSettingsMessages.Remove(msg);
            else if (RandomMessages.Contains(msg)) RandomMessages.Remove(msg);


            if (!msg.IsDeleted)
            {
                await msg.SourceMessage.DeleteAsync();
                msg.IsDeleted = true;
            }
        }
    }

    internal class TrackedMessage
    {
        internal RestUserMessage SourceMessage { get; set; }
        internal ulong TriggerById { get; set; }
        internal bool IsDeleted { get; set; }

        public TrackedMessage(RestUserMessage source, ulong triggerID)
        {
            SourceMessage = source;
            TriggerById = triggerID;
        }
    }

    internal class TimeoutTracker
    {
        internal SocketUser TrackedUser { get; set; }
        internal ulong GuildID { get; set; }

        public TimeoutTracker(SocketUser usr, ulong id)
        {
            TrackedUser = usr;
            GuildID = id;
        }
    }

    internal class TimeoutTimer
    {
        internal TimeoutTracker Tracker { get; set; }
        internal DateTime StartTime { get; }

        internal TimeoutTimer(TimeoutTracker timeout)
        {
            Tracker = timeout;
            StartTime = DateTime.Now;
        }
    }
}