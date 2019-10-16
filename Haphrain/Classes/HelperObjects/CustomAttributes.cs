using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Haphrain.Classes.HelperObjects
{
    internal class RequireBotOwner : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (!Constants._BOTOWNERS_.Contains(context.User.Id))
            {
                return Task.FromResult(PreconditionResult.FromError("Command can only be run by the owner of the bot."));
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
        }
    }
}
