using Discord;
using Discord.WebSocket;
using DiscordBot.Extensions;
using DiscordBot.Settings.Deserialized;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DiscordBot.Services
{
    class ReactRoleService
    {
        private ReactRoleSettings _reactSettings;
        private bool isRunning = false;

        private readonly Settings.Deserialized.Settings _settings;
        private readonly DiscordSocketClient _client;
        private readonly ILoggingService _loggingService;

        private IMessageChannel _messageChannel;
        // Dictionaries to simplify lookup
        private Dictionary<ulong, IUserMessage> ReactMessages = new Dictionary<ulong, IUserMessage>();
        // GuildRoles uses EmojiID as Key
        private Dictionary<ulong, IRole> GuildRoles = new Dictionary<ulong, IRole>();
        private Dictionary<ulong, GuildEmote> GuildEmotes = new Dictionary<ulong, GuildEmote>();

        private Dictionary<IGuildUser, ReactRoleUserData> _pendingUserUpdate = new Dictionary<IGuildUser, ReactRoleUserData>();
        class ReactRoleUserData
        {
            public IGuildUser user;
            public DateTime lastChange = DateTime.Now;
            public List<IRole> addRole = new List<IRole>();
            public List<IRole> removeRole = new List<IRole>();
            public ReactRoleUserData(IGuildUser id)
            {
                user = id;
            }
        }

        public ReactRoleService(DiscordSocketClient client, ILoggingService logging, Settings.Deserialized.Settings settings)
        {
            _loggingService = logging;
            _settings = settings;

            _client = client;
            _client.ReactionAdded += ReactionAdded;
            _client.ReactionRemoved += ReactionRemoved;

            // Event so we can Initialize
            _client.Ready += ClientIsReady;
        }
        /// <summary>
        /// Loads settings, this should just be message ids, and emotes/role ids
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                using (var file = File.OpenText(@"Settings/ReactionRoles.json"))
                {
                    _reactSettings = JsonConvert.DeserializeObject<ReactRoleSettings>(file.ReadToEnd());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to Deserialize 'ReactionRoles.Json' err: {ex.Message}");
                isRunning = false;
            }
        }

        private async Task ClientIsReady()
        {
            // This could be with all the others loading, but self-managed seems cleaner.
            LoadSettings();
            // Get the main channel we use
            _messageChannel = _client.GetChannel(_reactSettings.ChannelIndex) as IMessageChannel;
            // Get our Emotes
            //TODO Should probably make this work with global emotes as well?
            var ServerGuild = _client.GetGuild(_settings.guildId);
            foreach (var ReactList in _reactSettings.UserReactRoleList)
            {
                // Get The Message for this group of reactions
                if (!ReactMessages.ContainsKey(ReactList.MessageIndex))
                {
                    ReactMessages.Add(ReactList.MessageIndex, await _messageChannel.GetMessageAsync(ReactList.MessageIndex) as IUserMessage);
                }
                for (int i = 0; i < ReactList.RoleCount(); i++)
                {
                    // Add a Reference to our Roles to simplify lookup
                    if (!GuildRoles.ContainsKey(ReactList.Reactions[i].EmojiID))
                    {
                        GuildRoles.Add(ReactList.Reactions[i].EmojiID, ServerGuild.GetRole(ReactList.Reactions[i].RoleID));
                    }
                    // Same for the Emojis, saves constant look-arounds
                    if (!GuildEmotes.ContainsKey(ReactList.Reactions[i].EmojiID))
                    {
                        GuildEmotes.Add(ReactList.Reactions[i].EmojiID, await ServerGuild.GetEmoteAsync(ReactList.Reactions[i].EmojiID));
                    }
                    // If our message doesn't have the emote, we add it.
                    if (!ReactMessages[ReactList.MessageIndex].Reactions.ContainsKey(GuildEmotes[ReactList.Reactions[i].EmojiID]))
                    {
                        // We could add these in bulk, but that'd require a bit more setup
                        await ReactMessages[ReactList.MessageIndex].AddReactionAsync(GuildEmotes[ReactList.Reactions[i].EmojiID]);
                    }
                }
            }
            isRunning = true;
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!isRunning)
                return;
            if (ReactMessages.ContainsKey(message.Id))
            {
                ReactionChanged(reaction.User.Value as IGuildUser, reaction.Emote as Emote, true);
            }
        }
        private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!isRunning)
                return;
            if (ReactMessages.ContainsKey(message.Id))
            {
                if (ReactMessages.ContainsKey(message.Id))
                {
                    ReactionChanged(reaction.User.Value as IGuildUser, reaction.Emote as Emote, false);
                }
            }
        }
        private void ReactionChanged(IGuildUser user, Emote emote, bool state)
        {
            if (IsUserValid(user))
            {
                IRole targetRole;
                if (GuildRoles.TryGetValue(emote.Id, out targetRole))
                {
                    UpdatePendingRoles(user, targetRole, state);
                }
            }
        }

        /// <summary>
        /// Updates users roles in bulk, this prevents discord from crying.
        /// </summary>
        private async void UpdateUserRoles(IGuildUser user)
        {
            ReactRoleUserData userData;
            if (_pendingUserUpdate.TryGetValue(user, out userData))
            {
                // Wait for a bit to give user to choose all their roles.
                // we add a bit more to the end just so we don't always hit a second delay if they only selected 1 emote.
                await Task.Delay(_reactSettings.RoleAddDelay + 250);
                while (((DateTime.Now - userData.lastChange).TotalMilliseconds < _reactSettings.RoleAddDelay))
                {
                    await Task.Delay(2000);
                }
                // Strip out any changes we don't need to prevent additional calls
                // If changes were made to either add or remove, we make those changes.
                if (userData.addRole.Count > 0)
                {
                    for (int i = userData.addRole.Count - 1; i >= 0; i--)
                    {
                        if (user.RoleIds.Contains(userData.addRole[i].Id))
                        {
                            userData.addRole.RemoveAt(i);
                        }
                    }
                    await user.AddRolesAsync(userData.addRole);
                }
                if (userData.removeRole.Count > 0)
                {
                    for (int i = userData.removeRole.Count - 1; i >= 0; i--)
                    {
                        if (user.RoleIds.Contains(userData.removeRole[i].Id))
                        {
                            userData.removeRole.RemoveAt(i);
                        }
                    }
                    await user.RemoveRolesAsync(userData.removeRole);
                }
                if (_reactSettings.LogUpdates)
                    await _loggingService.LogAction($"{user.Username} Updated Roles.", false, true);

                _pendingUserUpdate.Remove(user);
            }
        }

        /// <summary>
        /// If a user is reacting to messages, they're added to a pending list of updates. Any time they react within the timeframe, it resets, and updates what roles need to be set.
        /// </summary>
        private void UpdatePendingRoles(IGuildUser user, IRole role, bool state)
        {
            // We check if the user has pending updates, if they don't we add them
            if (!_pendingUserUpdate.ContainsKey(user))
            {
                _pendingUserUpdate.Add(user, new ReactRoleUserData(user));
                UpdateUserRoles(user);
            }
            var userData = _pendingUserUpdate[user];
            // Update Change to prevent spamming
            userData.lastChange = DateTime.Now;
            // Add our change, make sure it isn't in our RemoveList
            if (state == true) 
            { 
                userData.addRole.Add(role);
                userData.removeRole.Remove(role);
            } 
            else
            {
                userData.addRole.Remove(role);
                userData.removeRole.Add(role);
            }
        }

        private bool IsUserValid(IGuildUser user)
        {
            if (user.IsBot)
                return false;
            return true;
        }
    }

    #region ReactRole Containers
    public class ReactRoleSettings
    {
        public ulong ChannelIndex;
        public int RoleAddDelay = 5000;
        public bool LogUpdates;
        public List<UserReactRoles> UserReactRoleList;
    }
    public class UserReactRoles
    {
        public ulong MessageIndex;
        public string Desc { get; set; }
        public List<ReactRole> Reactions;

        public int RoleCount() { return Reactions.Count; }
    }
    public class ReactRole
    {
        public ulong RoleID { get; set; }
        public ulong EmojiID { get; set; }
    }
    #endregion
}
