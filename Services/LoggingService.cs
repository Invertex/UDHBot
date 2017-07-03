using System;
using System.IO;
using Discord.Commands;
using Discord.WebSocket;

namespace DiscordBot
{
    public class LoggingService
    {
        private readonly DiscordSocketClient _client;

        public LoggingService(DiscordSocketClient client)
        {
            _client = client;
        }
        
        public async void LogAction(string action, bool logToFile = true, bool logToChannel = true)
        {
            if (logToChannel)
            {
                var channel = _client.GetChannel(Settings.GetBotAnnouncementChannel()) as ISocketMessageChannel;
               await channel.SendMessageAsync(action);
            }

            if (logToFile)
            {
                File.AppendAllText(SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) + @"\log.txt", $"[{DateTime.Now:d/M/yy HH:mm:ss}] {action} {Environment.NewLine}");
            }
        }
    }
}