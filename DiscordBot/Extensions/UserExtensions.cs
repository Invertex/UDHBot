using Discord.WebSocket;

namespace DiscordBot.Extensions;

public static class UserExtensions
{
    public static bool IsUserBotOrWebhook(this IUser user)
    {
        return user.IsBot || user.IsWebhook;
    }
    
    public static bool HasRoleGroup(this IUser user, SocketRole role) 
    {
        return HasRoleGroup(user, role.Id);
    }
    public static bool HasRoleGroup(this IUser user, ulong roleId)
    {
        return user is SocketGuildUser guildUser && guildUser.Roles.Any(x => x.Id == roleId);
    }
}