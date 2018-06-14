using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordBot.Services
{
    public class WorkService
    {
        private class Work
        {
            public IGuildUser User;
            public DateTime Added;
            public IMessage Message;
            public ISocketMessageChannel Channel;
            public ulong Role;

            public Work(IGuildUser user, DateTime added, IMessage message, ISocketMessageChannel iSocketMessageChannel, ulong role)
            {
                User = user;
                Added = added;
                Message = message;
                Channel = iSocketMessageChannel;
                Role = role;
            }
        }


        private static ulong _hiringChannel, _lookingForWorkChannel, _collaborationChannel;
        private static int _daysToBeLockedOut, _daysToRemoveMessage;

        private static readonly List<Work> allWork = new List<Work>();

        public WorkService()
        {
            _daysToBeLockedOut = SettingsHandler.LoadValueInt("daysToBeLockedOut", JsonFile.PayWork);
            _daysToRemoveMessage = SettingsHandler.LoadValueInt("daysToRemoveMessage", JsonFile.PayWork);
            _lookingForWorkChannel = SettingsHandler.LoadValueUlong("looking-for-work", JsonFile.PayWork);
            _hiringChannel = SettingsHandler.LoadValueUlong("hiring", JsonFile.PayWork);
            _collaborationChannel = SettingsHandler.LoadValueUlong("collaboration", JsonFile.PayWork);
        }

        public async void TimerUpdate()
        {
            if (allWork == null || allWork.Count <= 0) return;

            Work[] finishedWorks = allWork.Where(x => x.Added.Subtract(DateTime.Now).TotalSeconds >= _daysToBeLockedOut)
                .ToArray();
            Work[] finishedUserMute = finishedWorks.Where(x => x.User != null).ToArray();
            Work[] finishedMessages = finishedWorks
                .Where(x => x.Added.Subtract(DateTime.Now).TotalSeconds >= _daysToRemoveMessage).ToArray();

            foreach (Work w in finishedUserMute)
            {
                await UserAddAccess(w);
            }
            foreach (Work w in finishedMessages)
            {
                await MessageRemove(w);
                allWork.Remove(w);
            }
        }

        public async Task OnMessageAdded(SocketMessage messageParam)
        {
            IChannel channel = messageParam.Channel;
            Work work = null;
            bool flag = false;
            if (channel.Id == _lookingForWorkChannel)
            {
                if (messageParam.Author.IsBot)
                {
                    return;
                }
                flag = true;
                work = new Work(messageParam.Author as IGuildUser, messageParam.Timestamp.DateTime, messageParam, messageParam.Channel,
                    SettingsHandler.LoadValueUlong("looking-for-work:mute", JsonFile.PayWork));
                await UserRemoveAccess(work);
                await MessageRemove(work);
                await DuplicateUserMsg(work.Channel, work.User, messageParam.Timestamp, messageParam.Content, work.User.Username,
                    work.User.GetAvatarUrl());
            }
            else if (!flag && channel.Id == _hiringChannel)
            {
                if (messageParam.Author.IsBot)
                {
                    return;
                }
                flag = true;
                work = new Work(messageParam.Author as IGuildUser, messageParam.Timestamp.DateTime, messageParam, messageParam.Channel,
                    SettingsHandler.LoadValueUlong("hiring:mute", JsonFile.PayWork));
                await UserRemoveAccess(work);
                await MessageRemove(work);
                await DuplicateUserMsg(work.Channel, work.User, messageParam.Timestamp, messageParam.Content, work.User.Username,
                    work.User.GetAvatarUrl());
            }
            else if (!flag && channel.Id == _collaborationChannel)
            {
                if (messageParam.Author.IsBot)
                {
                    return;
                }
                flag = true;
                work = new Work(messageParam.Author as IGuildUser, messageParam.Timestamp.DateTime, messageParam, messageParam.Channel,
                    SettingsHandler.LoadValueUlong("collaboration:mute", JsonFile.PayWork));
                await UserRemoveAccess(work);
                await MessageRemove(work);
                await DuplicateUserMsg(work.Channel, work.User, messageParam.Timestamp, messageParam.Content, work.User.Username,
                    work.User.GetAvatarUrl());
            }
            if (flag) allWork.Add(work);
        }

        private async Task UserRemoveAccess(Work work)
        {
            if (work.User.RoleIds.Contains(work.Role)) return;
            await work.User.AddRoleAsync(work.User.Guild.GetRole(work.Role));
        }

        private async Task UserAddAccess(Work work)
        {
            if (!work.User.RoleIds.Contains(work.Role)) return;
            await work.User.RemoveRoleAsync(work.User.Guild.GetRole(work.Role));
        }

        private async Task MessageRemove(Work work)
        {
            //await work.Channel.DeleteMessagesAsync(new IMessage[] {work.Message});
        }

        private async Task DuplicateUserMsg(ISocketMessageChannel channel, IUser user, DateTimeOffset timestamp, string content,
            string username, string icon)
        {
            Console.WriteLine("before");
            icon = string.IsNullOrEmpty(icon) ? "https://cdn.discordapp.com/embed/avatars/0.png" : icon;

            var u = user as IGuildUser;
            IRole mainRole = null;
            foreach (ulong id in u.RoleIds)
            {
                IRole role = u.Guild.GetRole(id);
                if (mainRole == null)
                    mainRole = u.Guild.GetRole(id);
                else if (role.Position > mainRole.Position)
                {
                    mainRole = role;
                }
            }
            Color c = mainRole.Color;

            EmbedBuilder builder = new EmbedBuilder()
                .WithColor(c)
                .WithTimestamp(timestamp.UtcDateTime)
                .WithDescription(content)
                .WithAuthor(author =>
                {
                    author
                        .WithName(username)
                        .WithIconUrl(icon);
                });
            Console.WriteLine("after");
            Embed embed = builder.Build();
            await channel.SendMessageAsync("", false, embed);
        }
    }
}