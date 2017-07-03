using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBot
{
    class Program
    {
        private DiscordSocketClient _client;

        private CommandService _commands;
        private IServiceProvider _services;
        private IServiceCollection _serviceCollection;
        private LoggingService _logging;
        private DatabaseService _database;
        private ProfileService _profile;

        private string _token = "";

        static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 50
            });

            _commands = new CommandService();
            _logging = new LoggingService(_client);
            _database = new DatabaseService();
            _profile = new ProfileService(_database);
            _serviceCollection = new ServiceCollection();
            _serviceCollection.AddSingleton(_logging);
            _serviceCollection.AddSingleton(_database);
            _serviceCollection.AddSingleton(_profile);
            _services = _serviceCollection.BuildServiceProvider();
            
            
            await InstallCommands();

            _client.Log += Logger;

            // await InitCommands();

            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();

            _client.Ready += () =>
            {
                Console.WriteLine("Bot is connected");
                return Task.CompletedTask;
            };

            await Task.Delay(-1);
        }

        private static Task Logger(LogMessage message)
        {
            var cc = Console.ForegroundColor;
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
            Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message}");
            Console.ForegroundColor = cc;
            return Task.CompletedTask;
        }

        public async Task InstallCommands()
        {
            // Hook the MessageReceived Event into our Command Handler
            _client.MessageReceived += HandleCommand;
            _client.MessageReceived += _profile.UpdateXp;
            _client.MessageDeleted += (x, y) =>
            {
                _logging.LogAction($"{x.Value.Author.Username} has deleted message `{x.Value.Content}` from channel {y.Name}");
                return Task.CompletedTask;
            };
            
            // Discover all of the commands in this assembly and load them.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public async Task HandleCommand(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            var message = messageParam as SocketUserMessage;
            if (message == null)
                return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos)))
                return;
            // Create a Command Context
            var context = new CommandContext(_client, message);
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = await _commands.ExecuteAsync(context, argPos, _services);
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }
    }
}