﻿using Discord;
using Discord.Commands;
using Haphrain.Classes.HelperObjects;
using Haphrain.Classes.MortyGame;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace Haphrain.Classes.Commands
{
    public class MortyGameCmds : ModuleBase<SocketCommandContext>
    {
        [Command("mortystart"), Alias("ms"), Summary("Register the user to enable finding Mortys")]
        public async Task RegisterUser()
        {
            if (GlobalVars.RegisteredMortyUsers.Contains(Context.User.Id) || GlobalVars.MortyTimeouts.ContainsKey(Context.User.Id))
            {
                var m = await Context.Channel.SendMessageAsync($"Sorry {Context.User.Mention}, but you already did this!");
                GlobalVars.AddRandomTracker(m);
                return;
            }
            string sql = $"INSERT INTO mortyUsers (discordID) VALUES ({Context.User.Id})";
            DBControl.UpdateDB(sql);

            GlobalVars.RegisteredMortyUsers.Add(Context.User.Id);
            GlobalVars.MortyTimeouts.Add(Context.User.Id, false);

            string prefix = GlobalVars.GuildOptions.SingleOrDefault(x => x.GuildID == Context.Guild.Id).Prefix;
            await Context.Channel.SendMessageAsync($"Hi {Context.User.Mention}, you can now use `{prefix}morty` in order to start gathering your Mortys!");
        }

        [Command("mortyinfo"), Alias("mi"), Summary("View information about base Morty by name")]
        public async Task MortyInfo([Remainder]string name)
        {
            Character c = GlobalVars.GameObj.MortyList.SingleOrDefault(x => x.CharName.ToLower() == name.ToLower());
            await DoLookup(c, -1, name);
        }
        [Command("mortyinfo"), Alias("mi"), Summary("View information about base Morty by ID")]
        public async Task MortyInfo(int ID)
        {
            Character c = GlobalVars.GameObj.MortyList.SingleOrDefault(x => x.CharID == short.Parse(ID.ToString()));
            await DoLookup(c, ID);
        }

        [Command("morty"), Summary("Try your luck at obtaining a random Morty!")]
        public async Task SpawnMorty()
        {
            if (!GlobalVars.RegisteredMortyUsers.Contains(Context.User.Id))
            {
                string prefix = GlobalVars.GuildOptions.SingleOrDefault(x => x.GuildID == Context.Guild.Id).Prefix;
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you haven't registered to play yet.\nPlease use the command `{prefix}mortystart` first!");
                return;
            }

            if (GlobalVars.MortyTimeouts[Context.User.Id])
            {
                var m = await Context.Channel.SendMessageAsync($"{Context.User.Mention}, your portal gun is out of energy. Please wait for it to recharge!");
                GlobalVars.AddRandomTracker(m);
                return;
            }

            Random r = new Random();
            int rand = r.Next(1, 101);
            string rarity =
                rand <= 60 ? "Common" :
                rand <= 80 ? "Rare" :
                rand <= 84 ? "Epic" :
                rand <= 85 ? "Exotic" : "None";

            List<Character> WeightedList = rarity == "None" ? null : GlobalVars.GameObj.MortyList.Where(x => x.Rarity == rarity).ToList();
            Character c = WeightedList?[r.Next(WeightedList.Count())];

            if (c != null)
            {
                string sql =
                $"INSERT INTO ownedMortys (userID, mortyID) VALUES (" +
                $"(SELECT userID FROM mortyUsers WHERE discordID = {Context.User.Id})," +
                $"{c.CharID - 1})";
                DBControl.UpdateDB(sql);

                EmbedBuilder eb = new EmbedBuilder();
                string cName = c.CharName.Replace("'", "").Replace(" ", "").Replace("-", "");
                if (cName.ToLower() == "morty") cName = "MortyDefault";
                else
                {
                    string t = "Morty" + cName.Replace("Morty", "");
                    cName = t;
                }
                string fileName = $"{Constants._WORKDIR_}\\Assets\\{c.CharID}-{cName}.png";

                eb.WithAuthor("Pocket Mortys");
                eb.WithImageUrl($"attachment://{c.CharID}-{cName}.png");
                eb.WithTitle($"{Context.User} has found a [#{c.CharID}] {c.CharName}");
                eb.WithDescription($"" +
                    $"**Type**: {c.Type}\n" +
                    $"**Rarity**: {c.Rarity}\n" +
                    $"**Dimension**: {c.Dimension}\n" +
                    $"**Base Hitpoints**: {c.HP}\n" +
                    $"**Base Attack**: {c.ATK}\n" +
                    $"**Base Defense**: {c.DEF}\n" +
                    $"**Base Speed**: {c.SPD}\n" +
                    $"**Base Stat Total**: {c.StatTotal}");
                eb.WithFooter("pocketmortys.net");

                switch (c.Dimension)
                {
                    case "Mortyland":
                        eb.WithColor(57, 214, 33);
                        break;
                    case "Plumbubo Prime 51b":
                        eb.WithColor(252, 102, 252);
                        break;
                    case "Mortopia":
                        eb.WithColor(252, 216, 56);
                        break;
                    case "GF Mortanic":
                        eb.WithColor(170, 193, 204);
                        break;
                    default:
                        eb.WithColor(53, 53, 53);
                        break;
                }

                await Context.Channel.SendFileAsync(fileName, null, false, eb.Build());
            }
            else
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, it seems like you were looking in the wrong dimensions...\nYou couldn't find any Mortys!");

            GlobalVars.MortyTimeouts[Context.User.Id] = true;

            Timer ti = new Timer();
            void handler(object sender, ElapsedEventArgs e)
            {
                ti.Stop();
                GlobalVars.MortyTimeouts[Context.User.Id] = false;
                ti.Dispose();
            }
            ti.StartTimer(handler, 3600000);
        }

        [Command("mortylist"), Alias("ml"), Summary("View a list of your Mortys!")]
        public async Task ListMortys(int page = 1)
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.Title = $"{Context.User}'s Mortys";
            string desc = "";

            var dbSettings = Program.dbSettings;

            #region Grab List
            SqlConnectionStringBuilder sBuilder = new SqlConnectionStringBuilder();
            sBuilder.InitialCatalog = dbSettings.db;
            sBuilder.UserID = dbSettings.username;
            sBuilder.Password = dbSettings.password;
            sBuilder.DataSource = dbSettings.host + @"\" + dbSettings.instance + "," + dbSettings.port;
            sBuilder.ConnectTimeout = 30;
            sBuilder.IntegratedSecurity = false;
            SqlConnection conn = new SqlConnection();

            conn.ConnectionString = sBuilder.ConnectionString;

            using (conn)
            {
                conn.Open();

                string sql =
                $"SELECT" +
                $" U.userID, C.mortyName, O.mortyLevel" +
                $" FROM ownedMortys O" +
                $" LEFT JOIN mortyCharacters C ON O.mortyID = C.mortyID" +
                $" LEFT JOIN mortyUsers U ON O.userID = U.userID" +
                $" WHERE U.discordID = {Context.User.Id}";

                SqlCommand cmd = new SqlCommand(sql, conn);
                SqlDataReader dr = cmd.ExecuteReader();
                Dictionary<string, int> mortyCounts = new Dictionary<string, int>();

                while (dr.Read())
                {
                    string name = dr.GetValue(1).ToString();
                    int lvl = int.Parse(dr.GetValue(2).ToString());

                    if (mortyCounts.ContainsKey(name))
                        mortyCounts[name]++;
                    else
                        mortyCounts.Add(name, 1);
                }

                List<string> results = new List<string>();
                foreach (var k in mortyCounts.Keys)
                {
                    results.Add($"**{k}** | Amount: {mortyCounts[k]}\n");
                }
                for (int i = (10*page)-10; i < 10 * page; i++)
                {
                    desc += i >= results.Count ? "" : results[i];
                }
                eb.WithDescription(desc);
                eb.WithFooter($"Page {page}/{(results.Count % 10 == 0 ? results.Count/10 : results.Count/10 +1)}");
                dr.Close();

                conn.Close();
                conn.Dispose();
                #endregion
            }
            await Context.Channel.SendMessageAsync(null, false, eb.Build());
        }

        private async Task DoLookup(Character c, int ID = -1, string name = "[Invalid ID/Name]")
        {
            if (c != null)
            {
                EmbedBuilder eb = new EmbedBuilder();
                string cName = c.CharName.Replace("'", "").Replace(" ", "").Replace("-", "").Replace(".","");
                if (cName.ToLower() == "morty") cName = "MortyDefault";
                else
                {
                    string t = "Morty" + cName.Replace("Morty", "");
                    cName = t;
                }
                string fileName = $"{Constants._WORKDIR_}\\Assets\\{c.CharID}-{cName}.png";

                eb.WithAuthor("Pocket Mortys");
                eb.WithImageUrl($"attachment://{c.CharID}-{cName}.png");
                eb.WithTitle($"**Base stats for** [#{c.CharID}] {c.CharName}");
                eb.WithDescription($"" +
                    $"**Type**: {c.Type}\n" +
                    $"**Rarity**: {c.Rarity}\n" +
                    $"**Dimension**: {c.Dimension}\n" +
                    $"**Base Hitpoints**: {c.HP}\n" +
                    $"**Base Attack**: {c.ATK}\n" +
                    $"**Base Defense**: {c.DEF}\n" +
                    $"**Base Speed**: {c.SPD}\n" +
                    $"**Base Stat Total**: {c.StatTotal}");
                eb.WithFooter("pocketmortys.net");

                switch (c.Dimension)
                {
                    case "Mortyland":
                        eb.WithColor(57, 214, 33);
                        break;
                    case "Plumbubo Prime 51b":
                        eb.WithColor(252, 102, 252);
                        break;
                    case "Mortopia":
                        eb.WithColor(252, 216, 56);
                        break;
                    case "GF Mortanic":
                        eb.WithColor(170, 193, 204);
                        break;
                    default:
                        eb.WithColor(53, 53, 53);
                        break;
                }

                await Context.Channel.SendFileAsync(fileName, null, false, eb.Build());
            }
            else
            {
                string[] Messages = new string[] 
                {
                    "Oh jeez, I couldn't find that Morty!",
                    "This Morty seems to have been swallowed by Dimension 404..."
                };
                var msg = await Context.Channel.SendMessageAsync($"{Messages[new Random().Next(0,Messages.Length)]}\n*(Make sure you didn't make a mistake!Invalid ID/Name)*");
                GlobalVars.AddRandomTracker(msg);
            }
        }
    }
}