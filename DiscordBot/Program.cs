using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Services;
using DiscordBot.Settings.Deserialized;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace DiscordBot
{
    public class Program
    {
        private DiscordSocketClient _client;

        private CommandService _commandService;
        private IServiceProvider _services;
        private CommandHandlingService _commandHandlingService;

        private static PayWork _payWork;
        private static Rules _rules;
        private static Settings.Deserialized.Settings _settings;
        private static UserSettings _userSettings;

        public static void Main(string[] args) =>
            new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            DeserializeSettings();

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 50
            });

            _commandService = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async
            });

            _services = ConfigureServices();
            _commandHandlingService = _services.GetRequiredService<CommandHandlingService>();
            _services.GetRequiredService<ModerationService>();

            await _commandHandlingService.Initialize();

            _client.Log += Logger;

            await _client.LoginAsync(TokenType.Bot, _settings.Token);
            await _client.StartAsync();

            _client.Ready += () =>
            {
                Console.WriteLine("Bot is connected");
                //_audio.Music();
                return Task.CompletedTask;
            };

            await Task.Delay(-1);
        }

        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(_settings)
                .AddSingleton(_rules)
                .AddSingleton(_payWork)
                .AddSingleton(_userSettings)
                .AddSingleton(_client)
                .AddSingleton(_commandService)
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<ILoggingService, LoggingService>()
                .AddSingleton<DatabaseService>()
                .AddSingleton<UserService>()
                .AddSingleton<ModerationService>()
                .AddSingleton<PublisherService>()
                .AddSingleton<FeedService>()
                .AddSingleton<UpdateService>()
                .AddSingleton<AudioService>()
                .AddSingleton<CurrencyService>()
                .AddSingleton<ReactRoleService>()
                .BuildServiceProvider();
        }

        private static Task Logger(LogMessage message)
        {
            ConsoleColor cc = Console.ForegroundColor;
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

        private static void DeserializeSettings()
        {
            using (var file = File.OpenText(@"Settings/Settings.json"))
            {
                _settings = JsonConvert.DeserializeObject<Settings.Deserialized.Settings>(file.ReadToEnd());
            }

            using (var file = File.OpenText(@"Settings/PayWork.json"))
            {
                _payWork = JsonConvert.DeserializeObject<PayWork>(file.ReadToEnd());
            }

            using (var file = File.OpenText(@"Settings/Rules.json"))
            {
                _rules = JsonConvert.DeserializeObject<Rules>(file.ReadToEnd());
            }

            using (var file = File.OpenText(@"Settings/UserSettings.json"))
            {
                _userSettings = JsonConvert.DeserializeObject<UserSettings>(file.ReadToEnd());
            }
        }
    }
}