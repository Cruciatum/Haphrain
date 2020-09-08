using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Haphrain.Classes.HelperObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haphrain.Classes.Commands
{
    public class OwnerSays : ModuleBase<SocketCommandContext>
    {
        [Command("say"), Summary("Allow bot creator to send a message to specified channel"), RequireBotOwner]
        public async Task Say(ulong chanId, [Remainder]string msg)
        {
            SocketTextChannel chan = (SocketTextChannel)Context.Client.GetChannel(chanId);
            await chan.SendMessageAsync(msg);
        }

        [Command("dm"), 
            Summary("Secretly send a Direct Message to the specified User/User ID OR the bot owner!"), 
            RequireBotPermission(ChannelPermission.ManageMessages, ErrorMessage = "USERMENTION, I can't delete messages here\nIf this was meant to be private, please delete it yourself!"), 
            Priority(0)]
        public async Task DirMsg(string usr, [Remainder]string msg)
        {
            ulong usrID;
            if (usr == "owner") { usrID = 187675380423852032; }
            else usrID = ulong.Parse(usr);

            IUser u = null;
            foreach (var g in GlobalVars.Client.Guilds)
            {
                var foundUsr = g.Users.SingleOrDefault(x => x.Id == usrID);
                if (foundUsr != null) { u = foundUsr; break; }
            }

            string eMsg = "";
            if (u == null) { u = Context.Message.Author; eMsg = $"I couldn't find this user, please make sure I share a guild with this user before attempting that command!"; }
            IDMChannel c = await u.GetOrCreateDMChannelAsync();
            while (c == null) { }
            if (eMsg == "")
            {
                var endMsg = $"**Message from:**\n {Context.Message.Author}\n**Content:**\n`{msg}`";
                if (Context.Message.Attachments.Count != 0) {
                    endMsg += $"\nAttached files:";
                    foreach (Attachment a in Context.Message.Attachments)
                    {
                        endMsg += $"\n{a.Url}";
                    }
                }
                await c.SendMessageAsync(endMsg);
            }
            else
            {
                var endMsg = $"**Error:**\n{eMsg}\n\nYour message was still deleted for privacy.\nYour message:\n{msg}";
                if (Context.Message.Attachments.Count != 0)
                {
                    endMsg += $"\nAttached files:";
                    foreach (Attachment a in Context.Message.Attachments)
                    {
                        endMsg += $"\n{a.Url}";
                    }
                }
                await c.SendMessageAsync(endMsg);
            }
                
            await Context.Message.DeleteAsync();
        }
        [Command("dm"), 
            Summary("Secretly send a Direct Message to the specified User/User ID OR the bot owner!"), 
            RequireBotPermission(ChannelPermission.ManageMessages, ErrorMessage = "USERMENTION, I can't delete messages here\nIf this was meant to be private, please delete it yourself!"), 
            Priority(1)]
        public async Task DirMsg(IUser usr, [Remainder]string msg) { await DirMsg(usr.Id.ToString(), msg); }

        [Command("get channels"), Summary("Send channel IDs to the bot owner, based on guild ID"), RequireBotOwner]
        public async Task GetChannels(ulong guildID)
        {
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

        [Command("friend add"), Alias("fa", "f add", "friend a"), Summary("Add a friend to the bot"), RequireBotOwner]
        public async Task AddFriend(ulong friendID)
        {
            var user = await CustomUserTypereader.GetUserFromID(friendID, Context.Client.Guilds);
            if (user != null)
            {
                GlobalVars.FriendUsers.Add(friendID, user);

                string sql = $"INSERT INTO Friends (UserID, DateAdded) VALUES ({friendID},'{DateTime.Now.Day}-{DateTime.Now.Month}-{DateTime.Now.Year}')";
                DBControl.UpdateDB(sql);

                await Context.Channel.SendMessageAsync($"Added user {user.Mention} as a friend.\nThey will no longer be timed out and will have more privileges in the future.");
            }
            else
            {
                var msg = await Context.Channel.SendMessageAsync($"User not found (ID: {friendID}");
                GlobalVars.AddRandomTracker(msg, 5);
            }
        }

        [Command("friend remove"), Alias("fr", "f remove", "friend r"), Summary("Add a friend to the bot"), RequireBotOwner]
        public async Task RemoveFriend(ulong friendID)
        {
            GlobalVars.FriendUsers.TryGetValue(friendID, out IUser user);
            if (user != null)
            {
                GlobalVars.FriendUsers.Remove(friendID);

                string sql = $"DELETE FROM Friends WHERE UserID = {friendID}";
                DBControl.UpdateDB(sql);

                await Context.Channel.SendMessageAsync($"Removed user {user.Mention} from my friends list.");
            }
            else
            {
                var msg = await Context.Channel.SendMessageAsync($"User is not a friend (ID: {friendID})");
                GlobalVars.AddRandomTracker(msg, 5);
            }
        }
        
        [Command("ignore add"), Alias("ia","i add", "ignore a"), Summary("Ignore a user due to abuse"), RequireBotOwner]
        public async Task AddIgnore(ulong idiotID)
        {
            var user = await CustomUserTypereader.GetUserFromID(idiotID, Context.Client.Guilds);
            if (user != null)
            {
                GlobalVars.IgnoredUsers.Add(idiotID, user);

                string sql = $"INSERT INTO Ignores (UserID, DateAdded) VALUES ({idiotID},'{DateTime.Now.Day}-{DateTime.Now.Month}-{DateTime.Now.Year}')";
                DBControl.UpdateDB(sql);

                await Context.Channel.SendMessageAsync($"Added user {user.Mention} to the ignore list.\nThey will now be ignored by me.");
            }
            else
            {
                var msg = await Context.Channel.SendMessageAsync($"User not found (ID: {idiotID})");
                GlobalVars.AddRandomTracker(msg, 5);
            }
        }

        [Command("ignore remove"), Alias("ir", "i remove", "ignore r"), Summary("Ignore a user due to abuse"), RequireBotOwner]
        public async Task RemoveIgnore(ulong idiotID)
        {
            GlobalVars.IgnoredUsers.TryGetValue(idiotID, out IUser user);
            if (user != null)
            {
                GlobalVars.IgnoredUsers.Remove(idiotID);

                string sql = $"DELETE FROM Ignores WHERE UserID = {idiotID}";
                DBControl.UpdateDB(sql);

                await Context.Channel.SendMessageAsync($"Removed user {user.Mention} from the ignore list.\nThey can now use commands again.");
            }
            else
            {
                var msg = await Context.Channel.SendMessageAsync($"User not ignored (ID: {idiotID})");
                GlobalVars.AddRandomTracker(msg, 5);
            }
        }

        [Command("addowner"), RequireBotOwner]
        public async Task AddOwner(ulong id)
        {
            string sql = $"INSERT INTO BotOwners (OwnerID) VALUES ({id})";
            var l = Constants._BOTOWNERS_.ToList();
            l.Add(id);
            Constants._BOTOWNERS_ = l.ToArray();
            DBControl.UpdateDB(sql);
            var m = await Context.Channel.SendMessageAsync($"User {CustomUserTypereader.GetUserFromID(id, Context.Client.Guilds).Result.Username} added as one of my Owners.");
            GlobalVars.AddRandomTracker(m, 5);
        }
        [Command("addowner"), RequireBotOwner]
        public async Task AddOwner(IUser usr)
        {
            await AddOwner(usr.Id);
        }
    }
}
