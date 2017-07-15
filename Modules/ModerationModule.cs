using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DiscordBot
{
    public class ModerationModule : ModuleBase
    {
        private readonly LoggingService _logging;

        public ModerationModule(LoggingService logging)
        {
            _logging = logging;
        }

        [Command("mute"), Summary("Mute a user for a fixed duration")]
        [Alias("shutup", "stfu")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        async Task MuteUser(IUser user, int arg)
        {
            var u = user as IGuildUser;

            await u.AddRoleAsync(Settings.GetMutedRole(Context.Guild));
            await ReplyAsync("User " + user + " has been muted for " + arg + " seconds.");
            await _logging.LogAction($"{Context.User.Username} has muted {u.Username} for {arg} seconds");

            await Context.Message.DeleteAsync();

            await Task.Delay(arg * 1000);
            await UnmuteUser(user);
        }

        [Command("unmute"), Summary("Unmute a muted user")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        async Task UnmuteUser(IUser user)
        {
            var u = user as IGuildUser;

            Context.Message?.DeleteAsync();

            await u.RemoveRoleAsync(Settings.GetMutedRole(Context.Guild));
            await ReplyAsync("User " + user + " has been unmuted.");
        }

        [Command("addrole"), Summary("Add a role to a user")]
        [Alias("roleadd")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        async Task AddRole(IRole role, IUser user)
        {
            if (!Settings.IsRoleAssignable(role))
            {
                await ReplyAsync("Role is not assigneable");
                return;
            }

            var u = user as IGuildUser;
            await u.AddRoleAsync(role);
            await ReplyAsync("Role " + role + " has been added to " + user);
            await _logging.LogAction($"{Context.User.Username} has added role {role} to {u.Username}");
        }

        [Command("removerole"), Summary("Remove a role from a user")]
        [Alias("roleremove")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        async Task RemoveRole(IRole role, IUser user)
        {
            if (!Settings.IsRoleAssignable(role))
            {
                await ReplyAsync("Role is not assigneable");
                return;
            }

            var u = user as IGuildUser;

            await u.RemoveRoleAsync(role);
            await ReplyAsync("Role " + role + " has been removed from " + user);
            await _logging.LogAction($"{Context.User.Username} has removed role {role} from {u.Username}");
        }

        [Command("clear"), Summary("Remove last x messages")]
        [Alias("clean", "nuke", "purge")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        async Task ClearMessages(int count)
        {
            ITextChannel channel = Context.Channel as ITextChannel;

            var messages = await channel.GetMessagesAsync(count + 1).Flatten();
            await channel.DeleteMessagesAsync(messages);

            var m = await ReplyAsync("Messages deleted.");
            await Task.Delay(5000);
            await m.DeleteAsync();
            await _logging.LogAction($"{Context.User.Username} has removed {count} messages from {Context.Channel.Name}");
        }

        [Command("kick"), Summary("Kick a user")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        async Task KickUser(IUser user)
        {
            var u = user as IGuildUser;

            await u.KickAsync();
            await _logging.LogAction($"{Context.User.Username} has kicked {u.Username}");
        }

        [Command("ban"), Summary("Ban an user")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        async Task BanUser(IUser user)
        {
            await Context.Guild.AddBanAsync(user, 7, RequestOptions.Default);
            await _logging.LogAction($"{Context.User.Username} has banned {user.Username}");
        }
    }
}