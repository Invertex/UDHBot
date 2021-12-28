using System.Reflection;
using System.Text;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Attributes;
using DiscordBot.Settings;
using ParameterInfo = Discord.Commands.ParameterInfo;

namespace DiscordBot.Services;

public class CommandHandlingService
{
    public bool IsInitialized { get; private set; }

    private readonly DiscordSocketClient _client;
    private readonly CommandService _commandService;
    private readonly IServiceProvider _services;
    private readonly BotSettings _settings;

    // While not the most attractive solution, it works, and is fairly cheap compared to the last solution.
    // Tuple of string moduleName, bool orderByName = false, bool includeArgs = true, bool includeModuleName = true for a dictionary
    private readonly Dictionary<(string moduleName, bool orderByName, bool includeArgs, bool includeModuleName), string> _commandList = new();
    private readonly Dictionary<(string moduleName, bool orderByName, bool includeArgs, bool includeModuleName), List<string>> _commandListMessages = new();

    public CommandHandlingService(
        DiscordSocketClient client,
        CommandService commandService,
        IServiceProvider services,
        BotSettings settings
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

    /// <summary> Generates a command list that can provide users with information. Commands require [Command][Summary] and [Priority](If not ordering by name)
    /// The results are cached, so this method can be called frequently without performance issues.</summary>
    /// <returns> List of strings that can be sent to the user without worry of being over the message length limit.</returns>
    public List<string> GetCommandListMessages(string moduleName, bool orderByName = false, bool includeArgs = true, bool includeModuleName = true)
    {
        var tupleKey = (moduleName, orderByName, includeArgs, includeModuleName);
        if (!_commandListMessages.TryGetValue(tupleKey, out List<string> commandResults))
        {
            GenerateCommandListOutputs(tupleKey);
            commandResults = _commandListMessages[tupleKey];
        }
        return commandResults;
    }

    /// <summary> Generates a command list that can provide users with information. Commands require [Command][Summary] and [Priority](If not ordering by name)
    /// The results are cached, so this method can be called frequently without performance issues.</summary>  <remarks>Strongly suggest using GetCommandListMessages</remarks>
    /// <returns>A large string with all the formatted commands, may be over text limits and shouldn't be sent directly to user.</returns>
    public string GetCommandList(string moduleName, bool orderByName = false, bool includeArgs = true, bool includeModuleName = true)
    {
        var tupleKey = (moduleName, orderByName, includeArgs, includeModuleName);
        if (!_commandList.TryGetValue(tupleKey, out string commandResults))
        {
            GenerateCommandListOutputs(tupleKey);
            commandResults = _commandList[tupleKey];
        }
        return commandResults;
    }

    private void GenerateCommandListOutputs(
        (string moduleName, bool orderByName, bool includeArgs, bool includeModuleName) input)
    {
        // If we don't have the command list, we need to build it.
        var commandList = new StringBuilder();
        commandList.Append($"__{input.moduleName} Commands__\n");

        // Generates a list of commands that doesn't include any that have the ``HideFromHelp`` attribute.
        var commands = _commandService.Commands.Where(x => x.Module.Name == input.moduleName && !x.Attributes.Contains(new HideFromHelpAttribute()));
        // Orders the list either by name or by priority, if no priority is given we push it to the end.
        commands = input.orderByName ? commands.OrderBy(c => c.Name) : commands.OrderBy(c => (c.Priority > 0 ? c.Priority : 1000));

        foreach (var c in commands)
        {
            commandList.Append($"**{(input.includeModuleName ? input.moduleName + " " : string.Empty)}{c.Name}** : {c.Summary} {GetArguments(input.includeArgs, c.Parameters)}\n");
        }
            
        string commandListString = commandList.ToString();
        _commandList[input]  = commandListString;
        _commandListMessages[input] = commandListString.MessageSplitToSize();
    }

    private string GetArguments(bool getArgs, IReadOnlyList<ParameterInfo> arguments)
    {
        if (!getArgs) return string.Empty;

        var args = string.Empty;
        foreach (var info in arguments)
        {
            args += $"`{info.Name}`{(info.IsOptional ? "\\*" : string.Empty)} ";
        }
        if (args.Length > 0)
            args = $"- args: *( {args})*";
        return args;
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

        if (!result.IsSuccess)
        {
            // If the whole message is only ! or ? or space
            if (message.Content.All(letter => letter is '!' or '?' or ' '))
                return;

            var resultString = result.ErrorReason;
            if (result is PreconditionGroupResult groupResult)
            {
                resultString = groupResult.PreconditionResults.First().ErrorReason;
            }
            await context.Channel.SendMessageAsync(resultString).DeleteAfterSeconds(10);
        }
    }
}