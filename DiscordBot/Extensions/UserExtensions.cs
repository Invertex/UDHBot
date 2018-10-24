using System.Linq;
using Discord.WebSocket;
using DiscordBot.Settings.Deserialized;

namespace DiscordBot.Extensions
{
    public static class UserExtensions
    {
        public static bool IsUserModSquad(this SocketGuildUser user, RoleModSquadPermission moderation)
        {
            if (user.Roles == null || user.Roles.Count == 0)
            {
                return false;
            }

            return user.Roles.Any(x => moderation.Roles.Contains(x.Name));
        }
    }
}