using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Extensions;

namespace DiscordBot.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly IServiceProvider _services;
        private readonly Settings.Deserialized.Settings _settings;

        public CommandHandlingService(
            DiscordSocketClient client,
            CommandService commandService,
            IServiceProvider services,
            Settings.Deserialized.Settings settings
        )
        {
            _client = client;
            _commandService = commandService;
            _services = services;
            _settings = settings;
            
            /*
             Event subscriptions
            */
            _client.MessageReceived += HandleCommand;
        }

        private async Task HandleCommand(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            if (!(messageParam is SocketUserMessage message))
                return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            char prefix = _settings.Prefix;
            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (!(message.HasCharPrefix(prefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos)))
                return;
            // Create a Command Context
            var context = new CommandContext(_client, message);
            // Execute the command. (result does not indicate a return value,
            // rather an object stating if the command executed successfully)
            var result = await _commandService.ExecuteAsync(context, argPos, _services);
            if (!result.IsSuccess)
            {
                await context.Channel.SendMessageAsync(result.ErrorReason).DeleteAfterSeconds(10);
            }
        }
    }
}