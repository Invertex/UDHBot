using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
        private UserService _user;
        private WorkService _work;
        private PublisherService _publisher;

        private string _token = "";

        static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
            _token = SettingsHandler.LoadValueString("token", JsonFile.Settings);

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 50
            });

            _commands = new CommandService();
            _logging = new LoggingService(_client);
            _database = new DatabaseService();
            _user = new UserService(_database, _logging);
            _work = new WorkService();
            _publisher = new PublisherService();
            _serviceCollection = new ServiceCollection();
            _serviceCollection.AddSingleton(_logging);
            _serviceCollection.AddSingleton(_database);
            _serviceCollection.AddSingleton(_user);
            _serviceCollection.AddSingleton(_work);
            _serviceCollection.AddSingleton(_publisher);
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

        private void OnUpdate(object obj)
        {
            _work.TimerUpdate();
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

        public async Task InstallCommands()
        {
            // Hook the MessageReceived Event into our Command Handler
            _client.MessageReceived += HandleCommand;
            _client.MessageReceived += _user.UpdateXp;
            _client.MessageReceived += _user.Thanks;
            _client.MessageReceived += _work.OnMessageAdded;
            _client.MessageDeleted += (x, y) =>
            {
                if (x.Value.Author.IsBot)
                    return Task.CompletedTask;

                _logging.LogAction(
                    $"{x.Value.Author.Username} has deleted message `{x.Value.Content}` from channel {y.Name}");
                return Task.CompletedTask;
            };
            _client.UserJoined += UserJoined;
            _client.GuildMemberUpdated += UserUpdated;
            _client.UserLeft += UserLeft;

            // Discover all of the commands in this assembly and load them.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());

            StringBuilder commandList = new StringBuilder();
            foreach (var c in _commands.Commands.Where(x => x.Module.Name == "UserModule"))
            {
                commandList.Append($"**{c.Name}** : {c.Summary}\n");
            }
            foreach (var c in _commands.Commands.Where(x => x.Module.Name == "role"))
            {
                commandList.Append($"**role {c.Name}** : {c.Summary}\n");
            }
            Settings.SetCommandList(commandList.ToString());
        }

        private async Task UserJoined(SocketGuildUser user)
        {
            _logging.LogAction($"User Joined: {user.Username}");
            ulong general = SettingsHandler.LoadValueUlong("generalChannel", JsonFile.Settings);
            Embed em = _user.WelcomeMessage(user.GetAvatarUrl(), user.Username, user.DiscriminatorValue);

            var socketTextChannel = _client.GetChannel(general) as SocketTextChannel;
            if (socketTextChannel != null)
                await socketTextChannel.SendMessageAsync(string.Empty, false, em);

            await _logging.LogAction(
                $"User Joined - {user.Mention} - `{user.Username}#{user.DiscriminatorValue}` - ID : `{user.Id}`");

            _database.AddNewUser(user);
            
            //TODO: add users when bot was offline
        }

        private async Task UserUpdated(SocketGuildUser oldUser, SocketGuildUser user)
        {
            if (oldUser.Nickname != user.Nickname)
            {
                _logging.LogAction(
                    $"User {oldUser.Nickname ?? oldUser.Username}#{oldUser.DiscriminatorValue} changed his " +
                    $"username to {user.Nickname ?? user.Username}#{user.DiscriminatorValue}");
                _database.UpdateUserName(user.Id, user.Nickname);
            }
            if (oldUser.AvatarId != user.AvatarId)
            {
                var avatar = user.GetAvatarUrl();
                _database.UpdateUserAvatar(user.Id, avatar);
            }
        }

        private async Task UserLeft(SocketGuildUser user)
        {
            DateTime joinDate;
            DateTime.TryParse(_database.GetUserJoinDate(user.Id), out joinDate);
            string timeStayed = (DateTime.Now - joinDate).ToString("dd days and HH hours");

            await _logging.LogAction(
                $"User Left - After {timeStayed} {user.Mention} - `{user.Username}#{user.DiscriminatorValue}` - ID : `{user.Id}`");
            _database.DeleteUser(user.Id);
        }

        public async Task HandleCommand(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            var message = messageParam as SocketUserMessage;
            if (message == null)
                return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            char prefix = SettingsHandler.LoadValueChar("prefix", JsonFile.Settings);
            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (!(message.HasCharPrefix(prefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos)))
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