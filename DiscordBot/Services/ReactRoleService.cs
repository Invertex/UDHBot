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

        public ReactRoleService(DiscordSocketClient client, ILoggingService logging, Settings.Deserialized.Settings settings)
        {
            _loggingService = logging;
            _settings = settings;

            _client = client;
            _client.ReactionAdded += ReationAdded;
            _client.ReactionRemoved += ReactionRemoved;

            // Event so we can Initialize
            _client.Ready += ClientIsReady;
        }

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
                    // If our message doesn't have the emote we've just added, we add it.
                    if (!ReactMessages[ReactList.MessageIndex].Reactions.ContainsKey(GuildEmotes[ReactList.Reactions[i].EmojiID]))
                    {
                        // We could add these in bulk, but that'd require a bit more setup
                        await ReactMessages[ReactList.MessageIndex].AddReactionAsync(GuildEmotes[ReactList.Reactions[i].EmojiID]);
                    }
                }
            }
        }

        private async Task ReationAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!isRunning)
                return;
            if (ReactMessages.ContainsKey(message.Id))
            {
                IGuildUser user = reaction.User.Value as IGuildUser;
                if (IsUserValid(user))
                {
                    // Check if their emote relates to a role and apply them.
                    Emote emote = reaction.Emote as Emote;
                    IRole targetRole;
                    if (GuildRoles.TryGetValue(emote.Id, out targetRole))
                    {
                        if (user.RoleIds.Contains(targetRole.Id))
                        {
                            return;
                        }
                        await user.AddRoleAsync(targetRole);
                        if (_reactSettings.LogUpdates)
                            await _loggingService.LogAction($"**{user.Username}** added role '**{targetRole.Name}**'.");
                    }
                }
            }
        }

        private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!isRunning)
                return;
            if (ReactMessages.ContainsKey(message.Id))
            {
                IGuildUser user = reaction.User.Value as IGuildUser;
                if (IsUserValid(user))
                {
                    // Check if their emote relates to a role and apply them.
                    Emote emote = reaction.Emote as Emote;
                    IRole targetRole;
                    if (GuildRoles.TryGetValue(emote.Id, out targetRole))
                    {
                        if (!user.RoleIds.Contains(targetRole.Id))
                        {
                            return;
                        }
                        await user.RemoveRoleAsync(targetRole);
                        if (_reactSettings.LogUpdates)
                            await _loggingService.LogAction($"**{user.Username}** removed role '**{targetRole.Name}**'.");
                    }
                }
            }
        }

        private bool IsUserValid(IGuildUser user)
        {
            if (user.IsBot || user.RoleIds.Contains(_settings.MutedRoleId))
                return false;
            return true;
        }
    }

    #region ReactRole Containers
    public class ReactRoleSettings
    {
        public ulong ChannelIndex;
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
