using Discord;
using Discord.Commands;
using Discord.Rest;
using Haphrain.Classes.Data;
using Haphrain.Classes.HelperObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Haphrain.Classes.Commands
{
    public class Emotes : ModuleBase<SocketCommandContext>
    {
        internal static readonly string RequestLocation = Constants._WORKDIR_ + Constants.slashType + "Requests" + Constants.slashType;
        private static readonly string FinalEmoteLocation = Constants._WORKDIR_ + Constants.slashType + "Emotes" + Constants.slashType;

        private readonly Random r = new Random();

        [Command("emote"), Alias("e")]
        public async Task SendEmote(string trigger, IUser usr = null)
        {
            List<ApprovedEmote> foundEmotes = new List<ApprovedEmote>();
            bool hasUsr = usr == null ? false : true;
            if (!hasUsr) usr = Context.Client.CurrentUser;
            if (Context.Message.Author.Id == usr.Id)
            {
                await Context.Channel.SendMessageAsync($"Why would you want to {trigger.ToLower()} yourself...?");
                return;
            }

            //Get all valid emotes for this trigger
            foreach (ApprovedEmote ae in GlobalVars.EmoteList.Values.Where(e => e.Trigger == trigger))
            {
                if (ae.RequiresTarget == hasUsr)
                {
                    foundEmotes.Add(ae);
                }
            }

            if (foundEmotes.Count > 0)
            {
                ApprovedEmote selected = foundEmotes[r.Next(0, foundEmotes.Count)];
                string msg = selected.OutputText;
                if (msg.ToLower().Contains("{author}"))
                    msg = msg.Replace("{author}", Context.User.Mention);
                else
                    msg = Context.User.Mention + " " + msg;
                if (usr != null)
                    msg = msg.Replace("{target}", usr.Mention);
                await Context.Channel.SendFileAsync(selected.FilePath, msg);
                var perms = Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions(Context.Channel as IGuildChannel);
                if (perms.ManageMessages)
                {
                    try { await Context.Message.DeleteAsync(); }
                    catch { }
                }
            }
            else
            {
                var prefix = GlobalVars.GuildOptions.SingleOrDefault(go => go.GuildID == Context.Guild.Id).Prefix;
                var tarString = "\"{target}\"";
                var m = await Context.Channel.SendMessageAsync($"Unknown emote, you can request this by using \n`{prefix}emote request {trigger} <imageURL> <true/false> <printed message>`\n"
                    + "Insert true if you want this to be a targetted emote, false if you don't\n"
                    + "Specify where you want the (optional) target's name in <printed message> by using " + tarString);

                GlobalVars.AddRandomTracker(m, 30);
            }
        }

        [Command("emote list"), Alias("e list", "el")]
        public async Task EmoteList()
        {
            EmbedBuilder eb = new EmbedBuilder().WithTitle("Available emotes");
            List<string> list = new List<string>();
            List<string> targetList = new List<string>();
            foreach (ApprovedEmote ae in GlobalVars.EmoteList.Values)
            {
                if (ae.RequiresTarget)
                    targetList.Add(ae.Trigger);
                else
                    list.Add(ae.Trigger);
            }

            list.Sort();
            eb.AddField("Self emotes: ", CraftList(list));

            targetList.Sort();
            eb.AddField("Targetted emotes: ", CraftList(targetList));
            await Context.Channel.SendMessageAsync(null,false,eb.Build());
        }
        private string CraftList(List<string> list)
        {
            list = list.Distinct().ToList();
            List<string> newList = new List<string>();
            foreach (string s in list)
            {
                newList.Add($"`{s}`");
            }
            return string.Join(" * ", newList);
        }

        [Command("emote accept"), Alias("ea", "e accept", "emote a"), RequireOwner]
        public async Task AcceptEmote(params string[] emoteIDs)
        {
            List<string> SuccessfulEmotes = new List<string>();
            string dir = FinalEmoteLocation.Substring(0, FinalEmoteLocation.Length - 1);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            foreach (string s in emoteIDs)
            {
                if (GlobalVars.EmoteRequests.TryGetValue(s, out EmoteRequest er))
                {
                    File.Move(RequestLocation + er.FileName, FinalEmoteLocation + er.FileName);
                    SuccessfulEmotes.Add(er.RequestID);
                    GlobalVars.EmoteList.Add(er.RequestID, new ApprovedEmote(er.RequestID, er.FileExtension, er.Trigger, er.RequiresTarget, er.OutputText));
                    GlobalVars.EmoteRequests.Remove(er.RequestID);
                    if (GlobalVars.RequestMessage.TryGetValue(er.RequestID, out var msg))
                    {
                        GlobalVars.RequestMessage.Remove(er.RequestID);
                        await msg.DeleteAsync();
                    }
                    string sql = $"INSERT INTO Emotes (EmoteID, RequestedBy, EmoteTrigger, fExt, RequireTarget, OutputText) VALUES ('{er.RequestID}', {er.RequestedBy}, '{er.Trigger}', '{er.FileExtension}', {(er.RequiresTarget ? 1 : 0)}, '{er.OutputText}');";
                    DBControl.UpdateDB(sql);
                }
            }

            await Context.Channel.SendMessageAsync($"Emote(s) ID(s) accepted: ({string.Join(", ", SuccessfulEmotes)})");
        }

        [Command("emote deny"), Alias("ed", "e deny", "emote d"), RequireOwner]
        public async Task DenyEmote(params string[] emoteIDs)
        {
            List<string> SuccessfulEmotes = new List<string>();
            foreach (string s in emoteIDs)
            {
                if (GlobalVars.EmoteRequests.TryGetValue(s, out EmoteRequest er))
                {
                    File.Delete(RequestLocation + er.FileName);
                    SuccessfulEmotes.Add(er.RequestID);
                    GlobalVars.EmoteRequests.Remove(er.RequestID);
                    if (GlobalVars.RequestMessage.TryGetValue(er.RequestID, out var msg))
                    {
                        GlobalVars.RequestMessage.Remove(er.RequestID);
                        await msg.DeleteAsync();
                    }
                }
            }

            await Context.Channel.SendMessageAsync($"Emote(s) ID(s) denied: ({string.Join(", ", SuccessfulEmotes)})");
        }

        [Command("emote request"), Alias("er", "e request", "emote r")]
        public async Task RequestEmote(string trigger, string url, bool RequiresTarget, [Remainder]string msg)
        {
            if (GlobalVars.FriendUsers.ContainsKey(Context.Message.Author.Id))
            {
                if (msg == "") msg = $"is {trigger}";
                EmoteRequest er = new EmoteRequest(Context.Message.Author, trigger, RequiresTarget, msg);
                string finalURL = "";
                string[] imgFileTypes = { ".jpg", ".jpeg", ".gif", ".png" };
                foreach (string s in imgFileTypes)
                {
                    if (url.Substring(url.Length - s.Length, s.Length) == s)
                    {
                        finalURL = url;
                        er.FileExtension = s;
                    }
                }
                if (finalURL == "")
                {
                    finalURL = url.Contains("tenor") ? ImageLogger.GetTenorGIF(url) : url.Contains("gfycat") ? ImageLogger.GetGfyCatAsync(url) : "";
                    foreach (string s in imgFileTypes)
                    {
                        if (finalURL.Substring(finalURL.Length - s.Length, s.Length) == s)
                        {
                            er.FileExtension = s;
                        }
                    }
                }

                if (finalURL != "")
                {
                    string dir = RequestLocation.Substring(0, RequestLocation.Length - 1);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    using (var c = new WebClient())
                    {
                        try
                        {
                            c.DownloadFile(finalURL, RequestLocation + er.FileName);
                        }
                        catch (Exception ex) { }
                        while (c.IsBusy) { }
                    }

                    GlobalVars.EmoteRequests.Add(er.RequestID, er);
                    
                    

                    try
                    {
                        await SendRequest(er);
                        var m = await Context.Channel.SendMessageAsync($"Emote requested, emote ID: {er.RequestID}");
                        GlobalVars.AddRandomTracker(m, 15);
                        var perms = Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions(Context.Channel as IGuildChannel);
                        if (perms.ManageMessages)
                        {
                            try { await Context.Message.DeleteAsync(); }
                            catch { }
                        }
                    }
                    catch (Exception ex) { Debug.WriteLine(ex.ToString()); }
                }
                else
                {
                    var m = await Context.Channel.SendMessageAsync("Could not get the download URL for this image.");
                    GlobalVars.AddRandomTracker(m, 20);
                }
            }
            else
            {
                var m = await Context.Channel.SendMessageAsync($"You're not on the whitelist, sorry.");
                GlobalVars.AddRandomTracker(m, 10);
            }
        }

        [Command("emote request edit"), Alias("ere", "e request edit", "emote r edit", "e r edit", "emote request e"), RequireOwner]
        public async Task EditRequest(string emoteID, string paramName, [Remainder]string newValue)
        {
            if (GlobalVars.EmoteRequests.TryGetValue(emoteID, out EmoteRequest er))
            {
                string msg = "";
                switch (paramName.ToLower())
                {
                    case "trigger":
                        GlobalVars.EmoteRequests.Values.Single(e => e.RequestID == er.RequestID).ChangeTrigger(newValue);
                        msg = $"Request {er.RequestID} has been edited.\nNew value for Trigger: `{newValue}`";
                        break;
                    case "requirestarget":
                        GlobalVars.EmoteRequests.Values.Single(e => e.RequestID == er.RequestID).RequiresTarget = (newValue == "true" ? true : false );
                        msg = $"Request {er.RequestID} has been edited.\n{(newValue == "true" ? "Will now require a target" : "Will no longer require a target!")}";
                        break;
                    case "outputtext":
                        GlobalVars.EmoteRequests.Values.Single(e => e.RequestID == er.RequestID).OutputText = newValue;
                        msg = $"Request {er.RequestID} has been edited.\nNew value for OutputText: `{newValue}`";
                        break;
                    default:
                        msg = $"Invalid parameter name. Requires `trigger`, `requirestarget` or `outputtext`.\nYou supplied {paramName}" +
                            $"";
                        break;
                }
                try
                {
                    var reqMsg = GlobalVars.RequestMessage.FirstOrDefault(p => p.Key == er.RequestID).Value;
                    await SendRequest(er);
                    await reqMsg.DeleteAsync();
                }
                catch { }
                var m = await Context.Channel.SendMessageAsync(msg);
                GlobalVars.AddRandomTracker(m, 10);
            }
        }

        private async Task SendRequest(EmoteRequest er)
        {
            EmbedBuilder eb = new EmbedBuilder()
                        .WithTitle($"Emote request by: {er.RequestedBy.Username}#{er.RequestedBy.Discriminator}")
                        .WithAuthor(er.RequestedBy)
                        .WithColor(Color.Orange);
            eb.AddField("Request ID:", er.RequestID);
            eb.AddField("Desired trigger:", er.Trigger);
            eb.AddField("Require user target:", er.RequiresTarget);
            eb.AddField("Output text:", er.OutputText.ToLower().Contains("{author}") ? er.OutputText : "{author} " + er.OutputText);

            var chan = Context.Client.GetChannel(623205967462662154) as IMessageChannel;
            string fName = RequestLocation + er.FileName;
            var reqMsg = await chan.SendFileAsync(fName, null, false, eb.Build());
            if (GlobalVars.RequestMessage.ContainsKey(er.RequestID))
                GlobalVars.RequestMessage.Remove(er.RequestID);
            GlobalVars.RequestMessage.Add(er.RequestID, reqMsg);
        }
    }

    public class ApprovedEmote
    {
        private static readonly string FinalEmoteLocation = Constants._WORKDIR_ + Constants.slashType + "Emotes" + Constants.slashType;
        public string EmoteID { get; }
        public string FilePath { get; }
        public string FileType { get; }
        public string Trigger { get; }
        public bool RequiresTarget { get; }
        public string OutputText { get; }

        public ApprovedEmote(string id, string fType, string trigger, bool b, string msg)
        {
            EmoteID = id;
            FileType = fType;
            Trigger = trigger;
            FilePath = FinalEmoteLocation + Trigger + "-" + EmoteID + FileType;
            RequiresTarget = b;
            if (msg.ToLower().Contains("{author}"))
                OutputText = msg;
            else
                OutputText = "{author} " + msg;
        }
    }

    public class EmoteRequest
    {
        public string RequestID { get; }
        public IUser RequestedBy { get; set; }
        public string Trigger { get; private set; }
        public string FileName { get { return $"{Trigger}-{RequestID}{FileExtension}"; } }
        public string FileExtension { get; set; }
        public bool RequiresTarget { get; set; }
        public string OutputText { get; set; }

        public EmoteRequest(IUser requester, string trigger, bool hasTarget, string msg)
        {
            RequestID = GenerateB64ID();
            RequestedBy = requester;
            Trigger = trigger;
            RequiresTarget = hasTarget;
            OutputText = msg;
        }

        private string GenerateB64ID()
        {
            string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string id = "";
            bool valid = false;
            while (!valid)
            {
                id = MakeID(alphabet);
                if (!GlobalVars.EmoteRequests.ContainsKey(id) && !GlobalVars.EmoteList.ContainsKey(id))
                {
                    valid = true;
                }
            }
            return id;
        }
        public void ChangeTrigger(string newTrigger)
        {
            File.Move(Emotes.RequestLocation + FileName, Emotes.RequestLocation + $"{newTrigger}-{RequestID}{FileExtension}");
            Trigger = newTrigger;
        }

        private readonly Random r = new Random();
        private string MakeID(string alphabet)
        {
            string s = "";
            for (int i = 0; i < 10; i++)
            {
                s += alphabet[r.Next(0, alphabet.Length)];
            }
            return s;
        }
    }
}