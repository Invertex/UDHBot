using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Services;
using DiscordBot.Settings;

namespace DiscordBot.Modules;

public class UnityHelpModule : ModuleBase
{
    #region Dependency Injection

    public UnityHelpService HelpService { get; set; }
    public UserService UserService { get; set; }
    public BotSettings BotSettings { get; set; }

    #endregion // Dependency Injection

    [Command("resolve"), Alias("complete")]
    [Summary("When a question is answered, use this command to mark it as resolved.")]
    public async Task ResolveAsync() 
    {
        if (!IsValidUser() || !IsInHelpChannel())
        {
            await Context.Message.DeleteAsync();
        }
        await HelpService.OnUserRequestChannelClose(Context.User, Context.Channel as SocketThreadChannel);
    }
    
    #region Utility

    bool IsInHelpChannel()
    {
        if (Context.Channel is SocketThreadChannel threadChannel)
        {
            return threadChannel.ParentChannel.Id == BotSettings.GenericHelpChannel.Id;
        }
        return false;
    }
    
    bool IsValidUser()
    {
        if (Context.User.IsBot || Context.User.IsWebhook)
            return false;
        return true;
    }

    #endregion // Utility
}