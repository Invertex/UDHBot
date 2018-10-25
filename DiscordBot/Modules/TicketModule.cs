using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot.Extensions;

// ReSharper disable all UnusedMember.Local
namespace DiscordBot.Modules
{
    public class TicketModule : ModuleBase {
        private Settings.Deserialized.Settings _settings;

        public TicketModule (Settings.Deserialized.Settings settings) {
            _settings = settings;
        }
        
        /// <summary>
        /// Creates a private channel only accessable by the mods, admins, and the user who used the command.
        ///
        /// One command, no args, simple.
        /// </summary>
        [Command("complain"), Alias("complains", "complaint"), Summary("Opens a private channel to complain. Syntax : !complain")]
        private async Task Complaint()
        {
            var channelList = Context.Guild.GetChannelsAsync().Result;
            var hash = Context.User.Id.ToString().GetSHA256().Substring(0, 8);
            var channelName =
                ParseToDiscordChannel(
                    $"{_settings.ComplaintChannelPrefix}-{hash}");
            var categoryExists = false;
            var categoryList = Context.Guild.GetCategoriesAsync().Result;
            var categoryName = _settings.ComplaintCategoryName;

            var everyonePerms = new OverwritePermissions(viewChannel: PermValue.Deny);
            var userPerms = new OverwritePermissions(viewChannel: PermValue.Allow);

            ulong? categoryId = null;

            await Context.Message.DeleteAsync();

            foreach (var category in categoryList)
            {
                if (string.Equals(category.Name, categoryName, StringComparison.CurrentCultureIgnoreCase))
                {
                    categoryId = category.Id;
                    categoryExists = true;
                    break;
                }
            }

            if (!categoryExists)
            {
                var category = Context.Guild.CreateCategoryAsync(categoryName);
                categoryId = category.Result.Id;
            }

            if (channelList.Any(channel => channel.Name == channelName))
            {
                await ReplyAsync($"{Context.User.Mention}, you already have an open complaint! Please use that channel!")
                    .DeleteAfterSeconds(15);
                return;
            }

            var newChannel = await Context.Guild.CreateTextChannelAsync(channelName, x =>
                x.CategoryId = categoryId
            );

            await newChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, everyonePerms);
            await newChannel.AddPermissionOverwriteAsync(Context.User, userPerms);
            await newChannel.AddPermissionOverwriteAsync(Context.Guild.Roles.First(r => r.Name == "Staff"), userPerms);
            await newChannel.AddPermissionOverwriteAsync(Context.Guild.Roles.First(r => r.Name == "Bot"), userPerms);

            await newChannel.SendMessageAsync(
                $"The content of this conversation will stay strictly between you {Context.User.Mention} and the staff.\n" +
                "Please stay civil, any insults or offensive language could see you punished.\n" +
                "Do not ping anyone and wait until a staff member is free to examine your complaint.");
            await newChannel.SendMessageAsync($"An administrator will be able to close this chat by doing !close.");

            /*await newChannel.SendMessageAsync(
                $"{Context.User.Mention}, this is your chat to voice your complaint to the staff members. When everything is finished between you and the staff, please do !close!");*/
        }

        /// <summary>
        /// Closes the ticket. No check on who used it, as unless a member of staff changes permissions, the only ones able to use this in a complaint
        /// channel is the user who created the channel and staff.
        /// </summary>
        [Command("close"), Alias("end", "done", "bye"), Summary("Closes the ticket")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        private async Task Close()
        {
            var channelName = _settings.ComplaintChannelPrefix;

            await Context.Message.DeleteAsync();

            if (Context.Channel.Name.StartsWith(channelName.ToLower()))
            {
                await Context.Guild.GetChannelAsync(Context.Channel.Id).Result.DeleteAsync();
            }
        }

        private string ParseToDiscordChannel(string channelName) => channelName.ToLower().Replace(" ", "-");
    }
}