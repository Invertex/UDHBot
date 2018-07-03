using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot.Extensions;

// ReSharper disable all UnusedMember.Local
namespace DiscordBot.Modules {
    public class TicketModule : ModuleBase {
        
        /// <summary>
        /// Creates a private channel only accessable by the mods, admins, and the user who used the command.
        ///
        /// One command, no args, simple.
        /// </summary>
        [Command("complaint"), Alias("complain", "new", "whine", "bitch", "moan"), Summary("Opens a private channel to complain.")]
        [RequireBotPermission(GuildPermission.SendMessages)]
        private async Task Complaint () {
            var channelList = Context.Guild.GetChannelsAsync().Result;
            var channelName = ParseToDiscordChannel($"{SettingsHandler.LoadValueString("complaintChannelPrefix", JsonFile.Settings)}-{Context.User.Username}");
            var categoryExists = false;
            var categoryList = Context.Guild.GetCategoriesAsync().Result;
            var categoryName = SettingsHandler.LoadValueString("complaintCategoryName", JsonFile.Settings);
            
            var everyonePerms = new OverwritePermissions(viewChannel: PermValue.Deny);
            var userPerms = new OverwritePermissions(viewChannel: PermValue.Allow);

            ulong? categoryId = null;

            await Context.Message.DeleteAsync();

            foreach (var category in categoryList) {
                if (string.Equals(category.Name, categoryName, StringComparison.CurrentCultureIgnoreCase)) {
                    categoryId = category.Id;
                    categoryExists = true;
                    break;
                }
            }

            if (!categoryExists) {
                var category = Context.Guild.CreateCategoryAsync(categoryName);
                categoryId = category.Result.Id;
            }

            if (channelList.Any(channel => channel.Name == channelName)) {
                await ReplyAsync($"{Context.User.Mention}, you already have an open complaint! Please use that channel!").DeleteAfterSeconds(15);
                return;
            }

            var newChannel = await Context.Guild.CreateTextChannelAsync(channelName, x =>
                x.CategoryId = categoryId
            );

            await newChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, everyonePerms);
            await newChannel.AddPermissionOverwriteAsync(Context.User, userPerms);

            await newChannel.SendMessageAsync(
                $"{Context.User.Mention}, this is your chat to voice your complaint to the staff members. When everything is finished between you and the staff, please do !close!");
        }

        /// <summary>
        /// Closes the ticket. No check on who used it, as unless a member of staff changes permissions, the only ones able to use this in a complaint
        /// channel is the user who created the channel and staff.
        /// </summary>
        [Command("close"), Alias("end", "done", "bye"), Summary("Closes the ticket")]
        [RequireBotPermission(GuildPermission.SendMessages)]
        private async Task Close () {
            var channelName = SettingsHandler.LoadValueString("complaintChannelPrefix", JsonFile.Settings);
            
            await Context.Message.DeleteAsync();
            
            if (Context.Channel.Name.StartsWith(channelName.ToLower())) {
                await Context.Guild.GetChannelAsync(Context.Channel.Id).Result.DeleteAsync();
            }
        }

        private string ParseToDiscordChannel (string channelName) => channelName.ToLower().Replace(" ", "-");
    }
}