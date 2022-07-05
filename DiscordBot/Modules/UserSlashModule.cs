using Discord.Interactions;
using DiscordBot.Services;
using DiscordBot.Settings;

namespace DiscordBot.Modules;

// For commands that only require a single interaction, these can be done automatically and don't require complex setup or configuration.
// ie; A command that might just return the result of a service method such as Ping, or Welcome
public class UserSlashModule : InteractionModuleBase
{
    #region Dependency Injection

    public CommandHandlingService CommandHandlingService { get; set; }
    public UserService UserService { get; set; }
    public BotSettings BotSettings { get; set; }

    #endregion

    #region Help

    [SlashCommand("help", "Shows available commands")]
    private async Task Help(string search = "")
    {
        await Context.Interaction.DeferAsync(ephemeral: true);

        var helpEmbed = HelpEmbed(0, search);
        if (helpEmbed.Item1 >= 0)
        {
            ComponentBuilder builder = new ComponentBuilder();
            builder.WithButton("Next Page", $"user_module_help_next:{0}");

            await Context.Interaction.FollowupAsync(embed: helpEmbed.Item2, ephemeral: true,
                components: builder.Build());
        }
        else
        {
            await Context.Interaction.FollowupAsync(embed: helpEmbed.Item2, ephemeral: true);
        }
    }

    [ComponentInteraction("user_module_help_next:*")]
    private async Task InteractionHelp(string pageString)
    {
        await Context.Interaction.DeferAsync(ephemeral: true);

        int page = int.Parse(pageString);

        var helpEmbed = HelpEmbed(page + 1);
        ComponentBuilder builder = new ComponentBuilder();
        builder.WithButton("Next Page", $"user_module_help_next:{helpEmbed.Item1}");

        await Context.Interaction.ModifyOriginalResponseAsync(msg =>
        {
            msg.Components = builder.Build();
            msg.Embed = helpEmbed.Item2;
        });
    }

    // Returns an embed with the help text for a module, if the page is outside the bounds (high) it will return to the first page.
    private (int, Embed) HelpEmbed(int page, string search = "")
    {
        EmbedBuilder embedBuilder = new EmbedBuilder();
        embedBuilder.Title = "User Module Commands";
        embedBuilder.Color = Color.LighterGrey;

        List<string> helpMessages = null;
        if (search == string.Empty)
        {
            helpMessages = CommandHandlingService.GetCommandListMessages("UserModule", false, true, false);

            if (page >= helpMessages.Count)
                page = 0;
            else if (page < 0)
                page = helpMessages.Count - 1;

            embedBuilder.WithFooter(text: $"Page {page + 1} of {helpMessages.Count}");
            embedBuilder.Description = helpMessages[page];
        }
        else
        {
            // We need search results which we don't cache, so we don't want to provide a page number
            page = -1;
            helpMessages = CommandHandlingService.SearchForCommand(("UserModule", false, true, false), search);
            if (helpMessages[0].Length > 0)
            {
                embedBuilder.WithFooter(text: $"Search results for {search}");
                embedBuilder.Description = helpMessages[0];
            }
            else
            {
                embedBuilder.WithFooter(text: $"No results for {search}");
                embedBuilder.Description = "No commands found";
            }
        }

        return (page, embedBuilder.Build());
    }

    #endregion

    [SlashCommand("welcome", "An introduction to the server!")]
    public async Task SlashWelcome()
    {
        await Context.Interaction.RespondAsync(string.Empty,
            embed: UserService.GetWelcomeEmbed(Context.User.Username), ephemeral: true);
    }

    [SlashCommand("ping", "Bot latency")]
    public async Task Ping()
    {
        await Context.Interaction.RespondAsync($"Bot latency: ...", ephemeral: true);
        await Context.Interaction.ModifyOriginalResponseAsync(m =>
            m.Content = $"Bot latency: {UserService.GetGatewayPing().ToString()}ms");
    }

    [SlashCommand("invite", "Returns the invite link for the server.")]
    public async Task ReturnInvite()
    {
        await Context.Interaction.RespondAsync(text: BotSettings.Invite, ephemeral: true);
    }

    #region Moderation

    [MessageCommand("Report Message")]
    public async Task ReportMessage(IMessage reportedMessage)
    {
        await Context.Interaction.DeferAsync(ephemeral: true);
        
        if (reportedMessage.Author.Id == Context.User.Id)
        {
            await Context.Interaction.FollowupAsync(text: "You can't report your own messages!", ephemeral: true);
            return;
        }
        if (reportedMessage.Author.IsBot) // Don't report bots
        {
            await Context.Interaction.FollowupAsync(text: "You can't report bot messages!", ephemeral: true);
            return;
        }
        if (reportedMessage.Author.IsWebhook) // Don't report webhooks
        {
            await Context.Interaction.FollowupAsync(text: "You can't report webhook messages!", ephemeral: true);
            return;
        }

        var reportedMessageChannel = await Context.Guild.GetTextChannelAsync(BotSettings.ReportedMessageChannel.Id);
        if (reportedMessageChannel == null)
            return;

        var embed = new EmbedBuilder();
        embed.WithTitle($"Message Reported");
        embed.WithDescription($"{Context.User.Username}#{Context.User.Discriminator} reported a message in #{Context.Channel.Name}. [GoTo]({reportedMessage.GetJumpUrl()})");
        embed.WithColor(new Color(0xFF0000));
        embed.AddField("Reported Content",
            $"User: {reportedMessage.Author.Username}#{reportedMessage.Author.Discriminator} - {reportedMessage.Author.Id}\n" +
            $"Content:\n{(reportedMessage.Content.Length > 200 ? reportedMessage.Content.Substring(0, 200) + "..." : reportedMessage.Content)}");
        
        // Links to any attachments included in the message so even if deleted, we can still see content
        if (reportedMessage.Attachments.Count > 0)
        {
            var attachments = reportedMessage.Attachments.Select(a => a.Url).ToList();
            string attachmentString = string.Empty;
            for (int i = 0; i < attachments.Count; i++)
            {
                attachmentString += $"[{i + 1}]({attachments[i]})";
                if (i < attachments.Count - 1)
                    attachmentString += "\n";
            }
            embed.AddField("Attachments", attachmentString);
        }
        await reportedMessageChannel.SendMessageAsync(string.Empty, embed: embed.Build());
        
        await Context.Interaction.ModifyOriginalResponseAsync(msg => msg.Content = $"Message has been reported.");
    }

    #endregion // Moderation

    #region User Roles

    [SlashCommand("roles", "Give or Remove roles for yourself (Programmer, Artist, Designer, etc)")]
    public async Task UserRoles()
    {
        await Context.Interaction.DeferAsync(ephemeral: true);

        ComponentBuilder builder = new ComponentBuilder();

        foreach (var userRole in BotSettings.UserAssignableRoles.Roles)
        {
            builder.WithButton(userRole, $"user_role_add:{userRole}");
        }

        builder.Build();

        await Context.Interaction.FollowupAsync(text: "Click any role that applies to you!", embed: null,
            ephemeral: true, components: builder.Build());
    }

    [ComponentInteraction("user_role_add:*")]
    public async Task UserRoleAdd(string role)
    {
        await Context.Interaction.DeferAsync(ephemeral: true);

        var user = Context.User as IGuildUser;
        var guild = Context.Guild;

        // Try get the role from the guild
        var roleObj = guild.Roles.FirstOrDefault(r => r.Name == role);
        if (roleObj == null)
        {
            await Context.Interaction.ModifyOriginalResponseAsync(msg =>
                msg.Content = $"Failed to add role {role}, role not found.");
            return;
        }
        // We make sure the role is in our UserAssignableRoles just in case
        if (BotSettings.UserAssignableRoles.Roles.Contains(roleObj.Name))
        {
            if (user.RoleIds.Contains(roleObj.Id))
            {
                await user.RemoveRoleAsync(roleObj);
                await Context.Interaction.ModifyOriginalResponseAsync(msg =>
                    msg.Content = $"{roleObj.Name} has been removed!");
            }
            else
            {
                await user.AddRoleAsync(roleObj);
                await Context.Interaction.ModifyOriginalResponseAsync(msg =>
                    msg.Content = $"You now have the {roleObj.Name} role!");
            }
        }
    }

    #endregion
}