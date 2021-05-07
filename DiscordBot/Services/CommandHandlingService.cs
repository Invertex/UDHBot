using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Extensions;

namespace DiscordBot.Services
{
    public class CommandHandlingService
    {
        public bool IsInitialized { get; private set; } = false;

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

        public async Task Initialize()
        {
            // Discover all of the commands in this assembly and load them.
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            IsInitialized = true;
        }

        /// <summary> Generates a command list that can provide users with information. Commands require [Command][Summary] and [Priority](If not ordering by name)</summary>
        public async Task<string> GetCommandList(string moduleName, bool orderByName = false, bool includeArgs = true)
        {
            // Simple wait.
            while (!IsInitialized)
                await Task.Delay(1000);
            
            var commandList = new StringBuilder();

            commandList.Append($"__{moduleName} Commands__\n");
            
            var commands = _commandService.Commands.Where(x => x.Module.Name == moduleName);
            if (!orderByName)
                commands = commands.OrderBy(c => c.Name);
            else
                commands = commands.OrderBy(c => c.Priority);
            
            foreach (var c in commands)
            {
                var args = "";
                if (includeArgs)
                    foreach (var info in c.Parameters) args += $"`{info.Name}`{(info.IsOptional ? "\\*" : string.Empty)} ";
                if (args.Length > 0)
                    args = $"- args: *( {args})*";

                commandList.Append($"**{moduleName} {c.Name}** : {c.Summary} {args}\n");
            }
            return commandList.ToString();
        }

        private async Task HandleCommand(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            if (!(messageParam is SocketUserMessage message))
                return;

            // Create a number to track where the prefix ends and the command begins
            var argPos = 0;
            var prefix = _settings.Prefix;
            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (!(message.HasCharPrefix(prefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos)))
                return;
            // Create a Command Context
            var context = new CommandContext(_client, message);
            // Execute the command. (result does not indicate a return value,
            // rather an object stating if the command executed successfully)
            var result = await _commandService.ExecuteAsync(context, argPos, _services);
            if (!result.IsSuccess) await context.Channel.SendMessageAsync(result.ErrorReason).DeleteAfterSeconds(10);
        }
    }
}