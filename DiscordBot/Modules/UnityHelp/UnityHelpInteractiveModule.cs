using Discord.Interactions;
using DiscordBot.Services;
using DiscordBot.Settings;
using Discord.WebSocket;

namespace DiscordBot.Modules;

public class UnityHelpInteractiveModule : InteractionModuleBase
{
    #region Dependency Injection

    public UnityHelpService HelpService { get; set; }
    public UserService UserService { get; set; }
    public BotSettings BotSettings { get; set; }

    #endregion // Dependency Injection

    [SlashCommand("resolve-question", "If in unity-help forum channel, resolve the thread")]
    public async Task ResolveQuestion()
    {
        await Context.Interaction.DeferAsync(ephemeral: true);

        if (!IsValidUser())
        {
            await Context.Interaction.RespondAsync("Invalid User", ephemeral: true);
            return;
        }

        if (!IsInHelpChannel())
        {
            await Context.Interaction.FollowupAsync(
                $"This command can only be used in <#{BotSettings.GenericHelpChannel.Id}> channels", ephemeral: true);
            return;
        }

        var response =
            await HelpService.OnUserRequestChannelClose(Context.User, Context.Channel as SocketThreadChannel);
        if (string.IsNullOrEmpty(response))
            await Context.Interaction.FollowupAsync(string.Empty, ephemeral: true);
        else
            await Context.Interaction.FollowupAsync(response, ephemeral: true);
    }

    #region Context Commands

    [MessageCommand("Mark as Correct Answer")]
    public async Task MarkResponseAnswer(IMessage targetResponse)
    {
        await Context.Interaction.DeferAsync(ephemeral: true);
        if (!IsValidUser())
        {
            await Context.Interaction.RespondAsync(string.Empty, ephemeral: true);
            return;
        }
        if (!IsInHelpChannel())
        {
            await Context.Interaction.FollowupAsync(
                $"This command can only be used in <#{BotSettings.GenericHelpChannel.Id}> channels", ephemeral: true);
            return;
        }

        if (targetResponse.Author == Context.User || targetResponse.Author.IsBot)
        {
            await Context.Interaction.FollowupAsync("You can't mark your own response as correct", ephemeral: true);
            return;
        }
        
        var response = await HelpService.MarkResponseAsAnswer(Context.User, targetResponse);
        if (string.IsNullOrEmpty(response))
            await Context.Interaction.FollowupAsync(string.Empty, ephemeral: true);
        else
            await Context.Interaction.FollowupAsync(response, ephemeral: true);
    }

    #endregion // Context Commands
    

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