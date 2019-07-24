using Discord;
using Discord.WebSocket;
using DiscordBot.Extensions;
using DiscordBot.Settings.Deserialized;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DiscordBot.Services
{
    class ReactRoleService
    {
        private readonly ulong MsgIndex = 603187631982903296; //TODO Add to settings?
        private readonly ulong MsgChannel = 603187532078776340; //TODO Add to settings?

        private IUserMessage _reactMessage = null;

        private readonly Settings.Deserialized.Settings _settings;
        private readonly DiscordSocketClient _client;
        private readonly ILoggingService _loggingService;

        private IMessageChannel _msgChannel = null;
        private IGuild _serverGuild;

        private UserReactRoles ReactRoles = null;

        private Dictionary<string, GuildEmote> ReactEmojis = new Dictionary<string, GuildEmote>();
        private Dictionary<string, IRole> EmojiRoles = new Dictionary<string, IRole>();
        private Dictionary<string, ulong> EmojiRoleID = new Dictionary<string, ulong>();

        public ReactRoleService(DiscordSocketClient client, ILoggingService logging, Settings.Deserialized.Settings settings)
        {
            _loggingService = logging;
            _settings = settings;

            _client = client;
            _client.ReactionAdded += ReationAdded;
            _client.ReactionRemoved += ReactionRemoved;

            // Event so we can Initialize
            _client.Ready += ClientIsReady;

            // Pull our junk from Settings
            ReactRoles = _settings.ReactRoles;
        }

        private async Task ClientIsReady()
        {
            _serverGuild = _client.GetGuild(_settings.guildId);
            if (_serverGuild == null)
            {
                //TODO Log this shit
            }
            // Get our Emotes
            for (int i = 0; i < ReactRoles.RoleCount(); i++)
            {
                ReactEmojis.Add(ReactRoles.EmojiID[i], await _serverGuild.GetEmoteAsync(ulong.Parse(ReactRoles.EmojiID[i])));
                EmojiRoles.Add(ReactEmojis[ReactRoles.EmojiID[i]].Name, _serverGuild.GetRole(ulong.Parse(ReactRoles.RoleID[i])));
                EmojiRoleID.Add(ReactEmojis[ReactRoles.EmojiID[i]].Name, ulong.Parse(ReactRoles.RoleID[i]));
                // We should add some more debugging for both of these
            }

            _msgChannel = _client.GetChannel(MsgChannel) as IMessageChannel;
            if (_msgChannel == null)
            {
                //TODO Log this shit
            }
            _reactMessage = await _msgChannel.GetMessageAsync(MsgIndex) as IUserMessage;
            if (_reactMessage == null)
            {
                //TODO log this shit
            }
            else
            {
                List<GuildEmote> MissingEmojis = new List<GuildEmote>();
                for (int i = 0; i < ReactRoles.RoleCount(); i++)
                {
                    // Role Emoji not added
                    if (!_reactMessage.Reactions.ContainsKey(ReactEmojis[ReactRoles.EmojiID[i]]))
                    {
                        MissingEmojis.Add(ReactEmojis[ReactRoles.EmojiID[i]]);
                    }
                }
                // We Apply all missing Emojis
                //TODO Should log the missing emojis?
                if (MissingEmojis.Count > 0)
                    await _reactMessage.AddReactionsAsync(MissingEmojis.ToArray());

                //? We could check if the message has more reacts than we have emojis and delete any extra emojis?

                // Just to reduce our RateLimit potential we self-limit
                // Timer t = new Timer(30000);
                // t.Elapsed += ReminderCheck;
                // t.Start();
            }
        }

        private async Task ReationAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (message.Id != MsgIndex)
                return;
            if (reaction.User.Value.IsBot)
                return;

            IGuildUser user = reaction.User.Value as IGuildUser;
            if (EmojiRoles.ContainsKey(reaction.Emote.Name))
            {
                if (user.RoleIds.Contains(EmojiRoleID[reaction.Emote.Name]))
                {
                    return;
                }
                await user.AddRoleAsync(EmojiRoles[reaction.Emote.Name]);
            }
        }

        private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (message.Id != MsgIndex)
                return;
            if (reaction.User.Value.IsBot)
                return;

            IGuildUser user = reaction.User.Value as IGuildUser;
            if (EmojiRoles.ContainsKey(reaction.Emote.Name))
            {
                if (!user.RoleIds.Contains(EmojiRoleID[reaction.Emote.Name]))
                {
                    return;
                }
                await user.RemoveRoleAsync(EmojiRoles[reaction.Emote.Name]);
            }
        }
    }
}
