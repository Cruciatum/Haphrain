using Discord.Rest;
using Discord.WebSocket;
using Haphrain.Classes.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;

namespace Haphrain
{
    internal static class GlobalVars
    {
        internal static DiscordSocketClient Client { get; set; }

        internal readonly static string GuildsFileLoc = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location).Replace(@"bin\Debug\netcoreapp2.1", @"Data\Guilds.xml");
        internal static XmlDocument GuildsFile { get; set; } = new XmlDocument();

        internal static List<TrackedMessage> TrackedLogChannelMessages { get; set; } = new List<TrackedMessage>(); //For changing log channel
        internal static List<TrackedMessage> TrackedSettingsMessages { get; set; } = new List<TrackedMessage>();

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
            t.StartTimer(handler, 60000);
            TrackedSettingsMessages.Add(tMsg);
        }

        internal static async Task UntrackMessage(TrackedMessage msg)
        {
            if (TrackedLogChannelMessages.Contains(msg)) TrackedLogChannelMessages.Remove(msg);
            else if (TrackedSettingsMessages.Contains(msg)) TrackedSettingsMessages.Remove(msg);


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
}
