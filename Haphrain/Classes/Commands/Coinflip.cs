using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Haphrain.Classes.Commands
{
    public class Coinflip : ModuleBase<SocketCommandContext>
    {
        [Command("coinflip"), Alias("cf", "coin", "flip"), Summary("Flip a coin")]
        public async Task cf()
        {
            int result = 0;
            Random r = new Random();
            result = r.Next(0, 100);

            Console.WriteLine($"{DateTime.Now} -> Executed coin flip, result: {result}");
            await Context.Channel.SendMessageAsync($"{Context.User.Mention} your coin landed on {(result % 2 == 0 ? "Heads" : "Tails")}");
        }
    }
}
