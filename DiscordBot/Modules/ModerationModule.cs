using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using DiscordBot.Extensions;
using DiscordBot.Services;
using DiscordBot.Settings.Deserialized;
using Pathoschild.NaturalTimeParser.Parser;

namespace DiscordBot.Modules
{
    public class ModerationModule : ModuleBase
    {
        private List<string> _commandList = new List<string>();
        
        private readonly DatabaseService _database;
        private readonly ILoggingService _logging;
        private readonly Rules _rules;
        private readonly Settings.Deserialized.Settings _settings;
        private readonly UserService _user;

        public ModerationModule(ILoggingService logging, UserService user,
                                DatabaseService database, Rules rules, Settings.Deserialized.Settings settings, CommandHandlingService commandHandlingService)
        {
            _logging = logging;
            _user = user;
            _database = database;
            _rules = rules;
            _settings = settings;
            Task.Run(async () =>
            {
                var commands =
                        await commandHandlingService.GetCommandList("ModerationModule", true, true, false);
                _commandList = commands.MessageSplitToSize();
            });
        }

        private Dictionary<ulong, DateTime> MutedUsers => _user.MutedUsers;

        [Command("mute")]
        [Summary("Mute a user for a fixed duration")]
        [Alias("shutup", "stfu")]
        [RequireModerator]
        public async Task MuteUser(IUser user, uint arg)
        {
            await Context.Message.DeleteAsync();

            var u = user as IGuildUser;
            if (u != null && u.RoleIds.Contains(_settings.MutedRoleId)) return;

            await u.AddRoleAsync(Context.Guild.GetRole(_settings.MutedRoleId));

            var reply = await ReplyAsync($"User {user} has been muted for {Utils.Utils.FormatTime(arg)} ({arg} seconds).");
            await _logging.LogAction(
                $"{Context.User.Username} has muted {u.Username} ({u.Id}) for {Utils.Utils.FormatTime(arg)} ({arg} seconds).");

            MutedUsers.AddCooldown(u.Id, (int) arg, ignoreExisting: true);

            await MutedUsers.AwaitCooldown(u.Id);
            await reply.DeleteAsync();
            await UnmuteUser(user, true);
        }

        [Command("mute")]
        [Summary("Mute a user for a fixed duration")]
        [Alias("shutup", "stfu")]
        [RequireModerator]
        public async Task MuteUser(IUser user, string naturalDuration, params string[] messages)
        {
            try
            {
                var dt = DateTime.Now.Offset(naturalDuration);
                if (dt < DateTime.Now)
                {
                    await ReplyAsync("Invalid DateTime specified.");
                    return;
                }

                await MuteUser(user, (uint) Math.Round((dt - DateTime.Now).TotalSeconds), messages);
            }
            catch (Exception)
            {
                await ReplyAsync("Invalid DateTime specified.");
                await Context.Message.DeleteAsync();
            }
        }

        [Command("mute")]
        [Summary("Mute a user for a fixed duration")]
        [Alias("shutup", "stfu")]
        [RequireModerator]
        public async Task MuteUser(IUser user, uint arg, params string[] messages)
        {
            var message = string.Join(' ', messages);

            await Context.Message.DeleteAsync();

            var u = user as IGuildUser;
            if (u != null && u.RoleIds.Contains(_settings.MutedRoleId)) return;

            await u.AddRoleAsync(Context.Guild.GetRole(_settings.MutedRoleId));

            var reply =
                await ReplyAsync($"User {user} has been muted for {Utils.Utils.FormatTime(arg)} ({arg} seconds). Reason : {message}");
            await _logging.LogAction(
                $"{Context.User.Username} has muted {u.Username} ({u.Id}) for {Utils.Utils.FormatTime(arg)} ({arg} seconds). Reason : {message}");
            var dm = await user.GetOrCreateDMChannelAsync(new RequestOptions());

            try
            {
                await dm.SendMessageAsync(
                    $"You have been muted from UDC for **{Utils.Utils.FormatTime(arg)}** for the following reason : **{message}**. " +
                    "This is not appealable and any tentative to avoid it will result in your permanent ban.", false,
                    null, new RequestOptions {RetryMode = RetryMode.RetryRatelimit, Timeout = 6000});
            }
            catch (HttpException)
            {
                await ReplyAsync($"Sorry {user.Mention}, seems I couldn't DM you because you blocked me !\n" +
                                 "I'll have to send your mute reason in public :wink:\n" +
                                 $"You have been muted from UDC for **{Utils.Utils.FormatTime(arg)}** for the following reason : **{message}**. " +
                                 "This is not appealable and any tentative to avoid it will result in your permanent ban.");
                await _logging.LogAction($"User {user.Username} has DM blocked and the mute reason couldn't be sent.", true, false);
            }

            MutedUsers.AddCooldown(u.Id, (int) arg, ignoreExisting: true);
            await MutedUsers.AwaitCooldown(u.Id);

            await UnmuteUser(user, true);
            reply?.DeleteAsync();
        }

        [Command("unmute")]
        [Summary("Unmute a muted user")]
        [RequireModerator]
        public async Task UnmuteUser(IUser user, bool fromMute = false)
        {
            var u = user as IGuildUser;

            if (!fromMute && u == Context.Message.Author)
            {
                await ReplyAsync("You can't unmute yourself.").DeleteAfterSeconds(30);
                return;
            }

            if (!fromMute && Context != null && Context.Message != null)
                await Context.Message.DeleteAsync();

            MutedUsers.Remove(user.Id);
            await u.RemoveRoleAsync(Context.Guild.GetRole(_settings.MutedRoleId));
            var reply = await ReplyAsync("User " + user + " has been unmuted.");
            // await Task.Delay(TimeSpan.FromSeconds(10d));
            reply?.DeleteAfterSeconds(10d);
        }

        [Command("addrole")]
        [Summary("Add a role to a user")]
        [Alias("roleadd")]
        [RequireModerator]
        public async Task AddRole(IRole role, IUser user)
        {
            var contextUser = Context.User as SocketGuildUser;
            await Context.Message.DeleteAsync();

            if (_settings.UserAssignableRoles.Roles.Contains(role.Name))
            {
                var u = user as IGuildUser;
                await u.AddRoleAsync(role);
                await ReplyAsync("Role " + role + " has been added to " + user).DeleteAfterTime(minutes: 5);
                await _logging.LogAction($"{contextUser.Username} has added role {role} to {u.Username}");
                return;
            }

            await ReplyAsync($"Bot cannot add {role.Name} role. Administrator must do it manually.").DeleteAfterSeconds(25);
        }

        [Command("removerole")]
        [Summary("Remove a role from a user")]
        [Alias("roleremove")]
        [RequireModerator]
        public async Task RemoveRole(IRole role, IUser user)
        {
            var contextUser = Context.User as SocketGuildUser;
            await Context.Message.DeleteAsync();

            if (_settings.UserAssignableRoles.Roles.Contains(role.Name))
            {
                var u = user as IGuildUser;

                await u.RemoveRoleAsync(role);
                await ReplyAsync("Role " + role + " has been removed from " + user).DeleteAfterTime(minutes: 5);
                await _logging.LogAction($"{contextUser.Username} has removed role {role} from {u.Username}");
                return;
            }

            await ReplyAsync($"Bot cannot remove {role.Name} role. Administrator must do it manually.").DeleteAfterSeconds(25);
        }

        [Command("clear")]
        [Summary("Remove last x messages")]
        [Alias("clean", "nuke", "purge")]
        [RequireModerator]
        public async Task ClearMessages(int count)
        {
            var channel = Context.Channel as ITextChannel;

            var messages = await channel.GetMessagesAsync(count + 1).FlattenAsync();
            await channel.DeleteMessagesAsync(messages);

            var m = await ReplyAsync("Messages deleted.");
            await Task.Delay(5000).ContinueWith(t =>
            {
                m.DeleteAsync();
                _logging.LogAction($"{Context.User.Username} has removed {count} messages from {Context.Channel.Name}");
            });
        }

        [Command("clear")]
        [Summary("Remove messages until the message at the specified id")]
        [Alias("clean", "nuke", "purge")]
        [RequireModerator]
        public async Task ClearMessages(ulong messageId)
        {
            var channel = Context.Channel as ITextChannel;

            var messages = await channel.GetMessagesAsync(messageId, Direction.After).FlattenAsync();
            await channel.DeleteMessagesAsync(messages);

            var m = await ReplyAsync("Messages deleted.");
            await Task.Delay(5000).ContinueWith(t =>
            {
                m.DeleteAsync();
                _logging.LogAction($"{Context.User.Username} has removed {messages.Count()} messages from {Context.Channel.Name}");
            });
        }

        [Command("kick")]
        [Summary("Kick a user")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        internal async Task KickUser(IUser user)
        {
            var u = user as IGuildUser;

            await u.KickAsync();
            await _logging.LogAction($"{Context.User.Username} has kicked {u.Username}");
        }

        [Command("ban")]
        [Summary("Ban an user")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task BanUser(IUser user, params string[] reasons)
        {
            var reason = string.Join(' ', reasons);
            await Context.Guild.AddBanAsync(user, 7, reason, RequestOptions.Default);
            await _logging.LogAction($"{Context.User.Username} has banned {user.Username}");
        }

        [Command("debug")]
        [Summary("Debug")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Debug(IUser user)
        {
            var guildUser = (IGuildUser) user;
            await ReplyAsync(guildUser.RoleIds.Count.ToString());
        }

        [Command("rules")]
        [Summary("Display rules of the current channel.")]
        [RequireModerator]
        public async Task Rules(int seconds = 60)
        {
            await Rules(Context.Channel, seconds);
            await Context.Message.DeleteAsync();
        }

        [Command("rules")]
        [Summary("Display rules of the mentionned channel.")]
        [RequireModerator]
        public async Task Rules(IMessageChannel channel, int seconds = 60)
        {
            //Display rules of this channel for x seconds
            var rule = _rules.Channel.First(x => x.Id == 0);
            IUserMessage m;
            await Context.User.GetOrCreateDMChannelAsync();
            if (rule == null)
                m = await ReplyAsync(
                    "There is no special rule for this channel.\nPlease follow global rules (you can get them by typing `!globalrules`)");
            else
                m = await ReplyAsync(
                    $"{rule.Header}{(rule.Content.Length > 0 ? rule.Content : "There is no special rule for this channel.\nPlease follow global rules (you can get them by typing `!globalrules`)")}");

            var deleteAsync = Context.Message?.DeleteAsync();
            if (deleteAsync != null) await deleteAsync;

            if (seconds == -1)
                return;
            await Task.Delay(seconds * 1000);
            await m.DeleteAsync();
        }

        [Command("globalrules")]
        [Summary("Display globalrules in current channel.")]
        [RequireModerator]
        public async Task GlobalRules(int seconds = 60)
        {
            //Display rules of this channel for x seconds
            var globalRules = _rules.Channel.First(x => x.Id == 0).Content;
            var m = await ReplyAsync(globalRules);
            await Context.Message.DeleteAsync();

            if (seconds == -1)
                return;
            await Task.Delay(seconds * 1000);
            await m.DeleteAsync();
        }

        [Command("channels")]
        [Summary("Get a description of the channels.")]
        [RequireModerator]
        public async Task ChannelsDescription(int seconds = 60)
        {
            //Display rules of this channel for x seconds
            var channelData = _rules.Channel;
            var sb = new StringBuilder();

            foreach (var c in channelData)
                sb.Append($"{(await Context.Guild.GetTextChannelAsync(c.Id))?.Mention} - {c.Header}\n");
            var text = sb.ToString();
            IUserMessage m;
            IUserMessage m2 = null;

            if (sb.ToString().Length > 2000)
            {
                m = await ReplyAsync(text.Substring(0, 2000));
                m2 = await ReplyAsync(text.Substring(2000));
            }
            else
                m = await ReplyAsync(text);

            await Context.Message.DeleteAsync();

            if (seconds == -1)
                return;
            await Task.Delay(seconds * 1000);
            await m.DeleteAsync();
            var deleteAsync = m2?.DeleteAsync();
            if (deleteAsync != null) await deleteAsync;
        }

        [Command("slowmode")]
        [Summary("Put on slowmode.")]
        [RequireModerator]
        public async Task SlowMode(int time)
        {
            await Context.Message.DeleteAsync();
            await (Context.Channel as ITextChannel).ModifyAsync(p => p.SlowModeInterval = time);
            await ReplyAsync($"Slowmode has been set to {time}s !").DeleteAfterSeconds(10);
        }

        [Command("tagrole")]
        [Summary("Tag a role and post a message.")]
        [Alias("mentionrole", "pingrole", "rolemention", "roletag", "roleping")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task TagRole(IRole role, params string[] messages)
        {
            var message = string.Join(' ', messages);
            var isMentionable = role.IsMentionable;
            if (!isMentionable) await role.ModifyAsync(properties => { properties.Mentionable = true; });
            await role.ModifyAsync(properties => { properties.Mentionable = true; });
            await Context.Channel.SendMessageAsync($"{role.Mention}\n{message}");
            if (!isMentionable) await role.ModifyAsync(properties => { properties.Mentionable = false; });
            await Context.Message.DeleteAsync();
        }

        [Command("react")]
        [Alias("reaction", "reactions", "addreactions", "addreaction")]
        [Summary("Adds the requested reactions to a message")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task React(ulong msgId, params string[] emojis)
        {
            var msg = (IUserMessage) await Context.Channel.GetMessageAsync(msgId);
            await Context.Message.DeleteAsync();
            foreach (var emoji in emojis)
                if (Emote.TryParse(emoji, out var emote))
                    await msg.AddReactionAsync(emote);
                else
                    await msg.AddReactionAsync(new Emoji(emoji));
        }

        [Command("react")]
        [Alias("reaction", "reactions", "addreactions", "addreaction")]
        [Summary("Adds the requested reactions to a message")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task React(params string[] emojis)
        {
            var msg = (IUserMessage) (await Context.Channel.GetMessagesAsync(2).FlattenAsync()).Last();

            await Context.Message.DeleteAsync();
            foreach (var emoji in emojis)
                if (Emote.TryParse(emoji, out var emote))
                    await msg.AddReactionAsync(emote);
                else
                    await msg.AddReactionAsync(new Emoji(emoji));
        }

        [Command("closepoll")]
        [Summary("Close a poll and append a message.")]
        [Alias("pollclose")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ClosePoll(IMessageChannel channel, ulong messageId, params string[] additionalNotes)
        {
            var additionalNote = string.Join(' ', additionalNotes);
            var message = (IUserMessage) await channel.GetMessageAsync(messageId);
            var reactions = message.Reactions;

            var reactionCount = "";
            foreach (var reaction in reactions)
                reactionCount += $" {reaction.Key.Name} ({reaction.Value.ReactionCount})";

            await message.ModifyAsync(properties =>
            {
                properties.Content = message.Content +
                                     $"\n\nThe poll has been closed. Here's the vote results :{reactionCount}\nAdditional notes : {additionalNote}";
            });
        }

        [Command("dbsync")]
        [Summary("Force add user to database")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task DbSync(IUser user)
        {
            await _database.AddNewUser((SocketGuildUser) user);
        }
        
        #region CommandList
        [RequireModerator]
        [Summary("Does what you see now.")]
        [Command("Mod Help")]
        public async Task ModerationHelp()
        {
            foreach (var message in _commandList)
            {
                await ReplyAsync(message);
            }
        }
        #endregion
    }
}