using Discord;
using Discord.Commands;
using Haphrain.Classes.MortyGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haphrain.Classes.Commands
{
    public class MortyGameCmds : ModuleBase<SocketCommandContext>
    {
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
            Random r = new Random();
            int rand = r.Next(1, 101);
            string rarity =
                rand <= 60 ? "Common" :
                rand <= 80 ? "Rare" :
                rand <= 84 ? "Epic" :
                rand <= 85 ? "Exotic" : "None";

            List<Character> WeightedList = rarity == "None" ? null : GlobalVars.GameObj.MortyList.Where(x => x.Rarity == rarity).ToList();
            Character c = WeightedList == null ? null : WeightedList[r.Next(WeightedList.Count())];
            string msg = c == null ? "You found nothing :(" : $"Morty found: {c.CharName}";
            await Context.Channel.SendMessageAsync(msg + $"\nYour roll: {rand} ({rarity})");
        }

        private async Task DoLookup(Character c, int ID = -1, string name = "EmptyStringIsBaller")
        {
            if (c != null)
            {
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
                eb.WithFooter("https://www.pocketmortys.net");

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
                var msg = await Context.Channel.SendMessageAsync($"Character {(ID == -1 ? $"with ID: {ID.ToString()}" : name)} not found. Make sure you don't have the wrong one!");
                GlobalVars.AddRandomTracker(msg);
            }
        }
    }
}
