using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordBot.Services
{
    public class ModerationService
    {
        private readonly DiscordSocketClient _client;
        private readonly Settings.Deserialized.Settings _settings;
        private readonly ILoggingService _loggingService;

        public ModerationService(DiscordSocketClient client, Settings.Deserialized.Settings settings, ILoggingService loggingService)
        {
            _client = client;
            _settings = settings;
            _loggingService = loggingService;
            
            _client.MessageDeleted += MessageDeleted;
        }

        private async Task MessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            if (message.Value.Author.IsBot || channel.Id == _settings.BotAnnouncementChannel.Id)
                return;

            var content = message.Value.Content;
            if (content.Length > 800)
                content = content.Substring(0, 800);

            EmbedBuilder builder = new EmbedBuilder()
                .WithColor(new Color(200, 128, 128))
                .WithTimestamp(message.Value.Timestamp)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"In channel {message.Value.Channel.Name}");
                })
                .WithAuthor(author =>
                {
                    author
                        .WithName($"{message.Value.Author.Username}");
                })
                .AddField("Deleted message", content);
            Embed embed = builder.Build();

            await _loggingService.LogAction(
                $"User {message.Value.Author.Username}#{message.Value.Author.DiscriminatorValue} has " +
                $"deleted the message\n{content}\n from channel #{channel.Name}", true, false);
            await _loggingService.LogAction(" ", false, true, embed);
        }
    }
}