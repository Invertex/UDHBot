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
       /* public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
                    SocketGuildUser user = context.Message.Author as SocketGuildUser;
            
            if (user.Roles.Any(x => x.Id == 228015486120624130))
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            return Task.FromResult(PreconditionResult.FromError(user + " attempted to use admin only command!"));
        }*/

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            SocketGuildUser user = context.Message.Author as SocketGuildUser;
            
            if (user.Roles.Any(x => x.Id == 228015486120624130))
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            return Task.FromResult(PreconditionResult.FromError(user + " attempted to use admin only command!"));
        }
    }
    
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RequireStaffAttribute : PreconditionAttribute
    {
       /* public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
                    SocketGuildUser user = context.Message.Author as SocketGuildUser;
            
            if (user.Roles.Any(x => x.Id == 228015486120624130))
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            return Task.FromResult(PreconditionResult.FromError(user + " attempted to use admin only command!"));
        }*/

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            SocketGuildUser user = context.Message.Author as SocketGuildUser;
            
            if (user.Roles.Any(x => x.Name == "Staff"))
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            return Task.FromResult(PreconditionResult.FromError(user + " attempted to use a staff only command!"));
        }
    }
}
