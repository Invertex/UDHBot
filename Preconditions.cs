using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace DiscordBot
{
    public class RequireAdminAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            throw new NotImplementedException();
        }
    }
}