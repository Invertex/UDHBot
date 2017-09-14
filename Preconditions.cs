using System;
using System.Linq;
using System.Threading.Tasks;

using Discord.Commands;
using Discord.WebSocket;

namespace DiscordBot
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RequireAdminAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            SocketGuild udh = await context.Client.GetGuildAsync(204951876960124928);
            SocketGuildUser user = udh.GetUser(context.User.Id);
            
            if (user.Roles.Any(x => x.Id == 228015486120624130))
            {
                return PreconditionResult.FromSuccess();
            }
            return PreconditionResult.FromError(user + " attempted to use admin only command!");
        }
    }
}
