using Discord.Commands;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Haphrain.Classes.Commands
{
    public class Coinflip : ModuleBase<SocketCommandContext>
    {
        Random r = new Random();
        [Command("coinflip"), Alias("cf", "coin", "flip"), Summary("Flip a coin")]
        public async Task Cf()
        {
            int result = 0;
            result = r.Next(0, 100);

            Console.WriteLine($"{DateTime.Now} -> Executed coin flip, result: {result}");
            await Context.Channel.SendMessageAsync($"{Context.User.Mention} your coin landed on {(result % 2 == 0 ? "Heads" : "Tails")}");
        }

        [Command("roll"), Summary("Roll any amount of dice of your choice")]
        public async Task Roll(string roll)
        {
            Regex regex = new Regex(@"^(\d+)[dD](\d+)([\+\-]?)(\d*)");

            if (!regex.IsMatch(roll))
            {
                var m = await Context.Channel.SendMessageAsync($"Invalid roll format.");
                GlobalVars.AddRandomTracker(m);
                return;
            }
            var match = regex.Match(roll);
            int amt = int.Parse(match.Groups[1].Value);
            int size = int.Parse(match.Groups[2].Value);
            int modifier = 0;
            string modSign = match.Groups[3].Value;

            if (modSign == "+")
            {
                modifier += int.Parse(match.Groups[4].Value);
            }
            else if (modSign == "-")
            {
                modifier -= int.Parse(match.Groups[4].Value);
            }
            else
                modSign = "";
                

            if (((long)amt * (long)size + (long)modifier) > int.MaxValue)
            {
                var m = await Context.Channel.SendMessageAsync($"A roll this size could end up breaking my poor brain, please divide your rolls or choose lower values.");
                GlobalVars.AddRandomTracker(m);
                return;
            }

            string result = "";
            int total = 0;

            for (int i = 0; i < amt; i++)
            {
                if (i > 0) result += ", ";
                int t = r.Next(1, size + 1);
                total += t;

                if (t == size)
                    result += $"**__{t.ToString()}__**";
                else if (t == 1)
                    result += $"__{t.ToString()}__";
                else
                    result += t.ToString();
            }

            await Context.Channel.SendMessageAsync($"{Context.User.Mention} has rolled {(total+modifier).ToString()} {(modifier != 0 ? $"*({total.ToString()} + {(modifier > 0 ? modifier.ToString() : $"({modifier.ToString()})")})*" : "")}: ({result}). ");
        }
    }
}
