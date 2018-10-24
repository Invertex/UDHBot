using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordBot.Services
{
    public class LoggingService : ILoggingService
    {
        private readonly DiscordSocketClient _client;

        private readonly Settings.Deserialized.Settings _settings;

        public LoggingService(DiscordSocketClient client, Settings.Deserialized.Settings settings)
        {
            _client = client;
            _settings = settings;
        }

        public async Task LogAction(string action, bool logToFile = true, bool logToChannel = true, Embed embed = null)
        {
            if (logToChannel)
            {
                var channel = _client.GetChannel(_settings.BotAnnouncementChannel.Id) as ISocketMessageChannel;
                await channel.SendMessageAsync(action, false, embed);
            }

            if (logToFile)
            {
                File.AppendAllText(_settings.ServerRootPath + @"/log.txt",
                    $"[{DateTime.Now:d/M/yy HH:mm:ss}] {action} {Environment.NewLine}");
            }
        }

        public void LogXp(string channel, string user, float baseXp, float bonusXp, float xpReduce, int totalXp)
        {
            File.AppendAllText(_settings.ServerRootPath + @"/logXP.txt",
                $"[{DateTime.Now:d/M/yy HH:mm:ss}] - {user} gained {totalXp}xp (base: {baseXp}, bonus : {bonusXp}, reduce : {xpReduce}) in channel {channel} {Environment.NewLine}");
        }
    }

    public interface ILoggingService
    {
        Task LogAction(string action, bool logToFile = true, bool logToChannel = true, Embed embed = null);
        void LogXp(string channel, string user, float baseXp, float bonusXp, float xpReduce, int totalXp);
    }
}