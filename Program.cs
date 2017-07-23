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
        private UpdateService _update;

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
                LogLevel = LogSeverity.Verbose,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 50
            });

            _commands = new CommandService(new CommandServiceConfig()
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async
            });
            _logging = new LoggingService(_client);
            _database = new DatabaseService(_logging);
            _user = new UserService(_database, _logging);
            _work = new WorkService();
            _publisher = new PublisherService(_client, _database);
            _update = new UpdateService(_logging, _publisher, _database);
            _serviceCollection = new ServiceCollection();
            _serviceCollection.AddSingleton(_logging);
            _serviceCollection.AddSingleton(_database);
            _serviceCollection.AddSingleton(_user);
            //_serviceCollection.AddSingleton(_work);
            //TODO: rework work service
            _serviceCollection.AddSingleton(_publisher);
            _serviceCollection.AddSingleton(_update);
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
            //_client.MessageReceived += _work.OnMessageAdded;
            _client.MessageDeleted += MessageDeleted;
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

        private async Task MessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            if (message.Value.Author.IsBot || channel.Id == Settings.GetBotAnnouncementChannel())
                return;


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
                .AddField("Deleted message", message.Value.Content);
            Embed embed = builder.Build();

            await _logging.LogAction(
                $"User {message.Value.Author.Username}#{message.Value.Author.DiscriminatorValue} has " +
                $"deleted the message\n{message.Value.Content}\n from channel #{channel.Name}", true, false);
            await _logging.LogAction(" ", false, true, embed);
        }


        private async Task UserJoined(SocketGuildUser user)
        {
            ulong general = SettingsHandler.LoadValueUlong("generalChannel/id", JsonFile.Settings);
            Embed em = _user.WelcomeMessage(user.GetAvatarUrl(), user.Username, user.DiscriminatorValue);

            var socketTextChannel = _client.GetChannel(general) as SocketTextChannel;
            if (socketTextChannel != null)
            {
                await socketTextChannel.SendMessageAsync(string.Empty, false, em);
            }

            await _logging.LogAction(
                $"User Joined - {user.Mention} - `{user.Username}#{user.DiscriminatorValue}` - ID : `{user.Id}`");

            _database.AddNewUser(user);
            
            string globalRules = Settings.GetRule(0).content;
            IDMChannel dm = await user.CreateDMChannelAsync();
            await dm.SendMessageAsync(
                "Hello and welcome to Unity Developer Hub !\nHope you enjoy your stay.\nHere are some rules to respect to keep the community friendly, please read them carefully.\n" +
                "Please also read the additional informations in the **#welcome** channel." +
                "You can get all the available commands on the server by typing !help in the **#bot-commands** channel.");
            await dm.SendMessageAsync(globalRules);

            //TODO: add users when bot was offline
        }

        private async Task UserUpdated(SocketGuildUser oldUser, SocketGuildUser user)
        {
            if (oldUser.Nickname != user.Nickname)
            {
                await _logging.LogAction(
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
            TimeSpan timeStayed = DateTime.Now - joinDate;
            await _logging.LogAction(
                $"User Left - After {(timeStayed.Days > 1 ? Math.Floor((double) timeStayed.Days).ToString() + " days" : " ")}" +
                $" {Math.Floor((double) timeStayed.Hours).ToString()} hours {user.Mention} - `{user.Username}#{user.DiscriminatorValue}` - ID : `{user.Id}`");
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
            {
                IUserMessage m = await context.Channel.SendMessageAsync(result.ErrorReason);
                await Task.Delay(10000);
                await m.DeleteAsync();
            }
        }
    }
}