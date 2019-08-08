using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Haphrain.Classes.Commands
{
    public class OwnerSays : ModuleBase<SocketCommandContext>
    {
        [Command("say"), Summary("Allow bot creator to send a message to specified channel")]
        public async Task Say(ulong chanId, [Remainder]string msg)
        {
            if (Context.User.Id != 187675380423852032)
            {
                var m = await Context.Channel.SendMessageAsync("Nice try...");
                GlobalVars.AddRandomTracker(m);
                return;
            }
            SocketTextChannel chan = (SocketTextChannel)Context.Client.GetChannel(chanId);
            await chan.SendMessageAsync(msg);
        }

        [Command("get channels"), Summary("Send channel IDs to the bot owner, based on guild ID")]
        public async Task GetChannels(ulong guildID)
        {
            if (Context.User.Id != 187675380423852032)
            {
                var m = await Context.Channel.SendMessageAsync("Nice try...");
                GlobalVars.AddRandomTracker(m);
                return;
            }
            SocketGuild guild = Context.Client.GetGuild(guildID);
            if (guild == null)
            {
                var m = await Context.Channel.SendMessageAsync($"Can't find guild from ID {guildID.ToString()}");
                GlobalVars.AddRandomTracker(m);
                return;
            }
            SocketGuildUser botOwner = Context.Client.GetGuild(604753009502584834).Owner;
            if (botOwner == null)
            {
                var m = await Context.Channel.SendMessageAsync("Can't find my owner");
                GlobalVars.AddRandomTracker(m);
                return;
            }
            var dmChan = await botOwner.GetOrCreateDMChannelAsync();

            if (guild.Channels.Count == 0)
            {
                var m = await Context.Channel.SendMessageAsync("Can't find any channels");
                GlobalVars.AddRandomTracker(m);
                return;
            }

            List<EmbedBuilder> eb = new List<EmbedBuilder>();
            float neededEmbeds = (float)guild.Channels.Count / 25f;
            for (float i = 0; i < neededEmbeds; i++)
            {
                eb.Add(new EmbedBuilder());
            }
            int x = 0;
            foreach (SocketGuildChannel c in guild.Channels)
            {
                if (c.GetType() == typeof(SocketTextChannel))
                {
                    eb[x].AddField($"{c.Name}", $"__{c.Id}__");
                }
                if (eb[x].Fields.Count == 25) x++;
            }

            if (eb[0].Fields.Count == 0)
                await dmChan.SendMessageAsync($"Can't get channel list for {guild.Name} ({guildID.ToString()})");
            else
            {
                foreach (EmbedBuilder builder in eb)
                    await dmChan.SendMessageAsync($"{guild.Name}({guildID}) - {eb.IndexOf(builder)+1}/{eb.Count}", false, builder.Build());
            }
        }
    }
}
