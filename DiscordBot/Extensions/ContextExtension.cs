using Discord.Commands;

namespace DiscordBot.Extensions;

public static class ContextExtension
{
    /// <summary>
    /// Returns true if the context includes a RoleID, UserID or Mentions Everyone (Should include @here, unsure)
    /// </summary>
    public static bool HasAnyPingableMention(this ICommandContext context)
    {
        return context.Message.MentionedUserIds.Count > 0 || context.Message.MentionedRoleIds.Count > 0 || context.Message.MentionedEveryone;
    }
}