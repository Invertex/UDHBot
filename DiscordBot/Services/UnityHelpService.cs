using System.Net.Http.Headers;
using Discord.WebSocket;
using DiscordBot.Settings;
using MessageExtensions = DiscordBot.Extensions.MessageExtensions;

namespace DiscordBot.Services;

public class ThreadContainer
{
    public ulong Owner { get; set; }
    public ulong FirstMessage { get; set; }
    public ulong ChannelId { get; set; }
    public ulong LatestMessage { get; set; }

    public bool IsResolved { get; set; } = false;
    public bool HasInteraction { get; set; } = false;
    
    
    public ulong BotsLastMessage { get; set; }
    public CancellationTokenSource CancellationToken { get; set; }
    public DateTime ExpectedShutdownTime { get; set; }
    
    // Additional Helpful ids
    public ulong NoAppliedTagsMessage { get; set; } = 0;
    public ulong WarningMessage { get; set; } = 0;
}

public class UnityHelpService
{
    private readonly DiscordSocketClient _client;
    private readonly ILoggingService _logging;
    
    #region Configuration
    
    private const int TimeBeforeClosedForResolvedTag = 1;
    private static readonly string ResolvedWarnOfPendingCloseMessage = $"This issue has been marked as resolved and will be archived in {TimeBeforeClosedForResolvedTag} minutes.";
    
    private const int HasResponseIdleTimeSelfUser = 60 * 4;
    private static readonly string HasResponseIdleTimeSelfUserMessage = $"Hello {{0}}! This forum has been inactive for {HasResponseIdleTimeSelfUser / 60} hours. If the question has been appropriately answered, click the {CloseEmoji} emoji to close this thread.";
    private const int HasResponseIdleTimeOtherUser = 60 * 8;
    private static readonly string HasResponseMessageRequestClose = $"Hello {{0}}! This forum has been inactive for {HasResponseIdleTimeOtherUser / 60} hours without your input. If the question has been appropriately answered, click the {CloseEmoji} emoji to close this thread.";
    private const string HasResponseExtraMessage = $"If you still need help, perhaps include additional details!";
    private static readonly Emoji CloseEmoji = new Emoji(":lock:");

    private const int NoResponseNotResolvedIdleTime = 1; // 60 * 24 * 2;
    private static readonly string StealthDeleteMessage = $"This question has been idle for {NoResponseNotResolvedIdleTime / 60} hours and has no response, it will be closed in {StealthDeleteTime / 60} hours if no other activity is detected.";
    private const int StealthDeleteTime = 2; // 60 * 5;
    
    private static readonly string NoAppliedTagsUsed = $"It looks like you haven't applied any tags to this thread, be sure to include the appropriate tags to help others find your thread!";
    
    private const int MinimumLengthMessage = 40;
    private readonly Embed _minimumLengthMessageEmbed = new EmbedBuilder()
        .WithTitle("Warning: Short Question!")
        .WithDescription($"Your question is short and may be difficult to answer! [don't ask to ask](https://dontasktoask.com/)!\n" +
                         $"- Errors? Include full error message.\n" +
                         $"- Features? Which version of Unity.\n" +
                         $"- Assets? Relevant store links.")
        .WithFooter("Be descriptive of the problem!")
        .WithColor(Color.LightOrange)
        .Build();

    #endregion // Configuration

    #region Extra Details
    
    private readonly IForumChannel _helpChannel;
    
    private const string ResolvedTag = "Resolved";
    private readonly ForumTag _resolvedForumTag;

    #endregion // Extra Details

    public UnityHelpService(DiscordSocketClient client, BotSettings settings, ILoggingService logging)
    {
        _client = client;
        _logging = logging;

        // get the help channel settings.GenericHelpChannel
        _helpChannel = _client.GetChannel(settings.GenericHelpChannel.Id) as IForumChannel;
        if (_helpChannel == null)
        {
            LoggingService.LogToConsole("[UnityHelpService] Help channel not found", LogSeverity.Error);
        }
        var resolvedTag = _helpChannel.Tags.FirstOrDefault(x => x.Name == ResolvedTag);
        _resolvedForumTag = resolvedTag;

        // on reaction added, call method
        _client.ReactionAdded += OnReactionAdded;

        _client.ThreadCreated += GatewayOnThreadCreated;
        _client.ThreadUpdated += GatewayOnThreadUpdated;
        _client.ThreadDeleted += GatewayOnThreadDeleted;
        _client.ThreadMemberJoined += GatewayOnThreadMemberJoinedThread;
        _client.ThreadMemberLeft += GatewayOnThreadMemberLeftThread;
        
        _client.MessageReceived += GatewayOnMessageReceived;
        _client.MessageUpdated += GatewayOnMessageUpdated;

        Task.Run(LoadActiveThreads);
    }

    private async Task LoadActiveThreads()
    {
        var _loadingActiveThreads = await GetHelpActiveThreads();
        foreach (var thread in _loadingActiveThreads)
        {
            var threadContents = (await _client.GetChannelAsync(thread.Id)) as SocketThreadChannel;
            var latestMessageId = (thread.MessageCount > 0 ? threadContents.GetMessagesAsync(1).FlattenAsync().Result.FirstOrDefault().Id : 0);
            var threadOwner = threadContents.Owner.Id;
            var threadContainer = new ThreadContainer
            {
                ChannelId = thread.Id,
                LatestMessage = latestMessageId,
                Owner = threadOwner,
                // TODO : (James) Work out if this should be resolved/closed already or not
                IsResolved = thread.AppliedTags.Contains(_resolvedForumTag.Id),
            };

            if (threadContainer.IsResolved)
            {
                // Run in new task so we don't block the other threads from being processed
                Task.Run(() => CloseThreadInTime(threadContainer, ResolvedWarnOfPendingCloseMessage,
                    TimeBeforeClosedForResolvedTag));
            }
            else
            {
                _activeThreads.Add(threadContainer.ChannelId, threadContainer);
            }
        }
    }

    #region Thread Tracking
    
    // Threads we're currently tracking
    private Dictionary<ulong, ThreadContainer> _activeThreads = new();

    #region Thread Creation
    
    private async Task OnThreadCreated(SocketThreadChannel thread)
    {
        ThreadContainer container = new()
        {
            ChannelId = thread.Id,
            LatestMessage = 0,
            Owner = thread.Owner.Id,
        };
        _activeThreads.Add(thread.Id, container);
        
        // Check message length and inform user if too short
        var firstMessage = (await thread.GetMessagesAsync(1).FlattenAsync()).FirstOrDefault();
        container.FirstMessage = firstMessage!.Id;
        if (firstMessage.Content.Length < MinimumLengthMessage)
        {
            var botResponse = await thread.SendMessageAsync(embed: _minimumLengthMessageEmbed);
            container.WarningMessage = botResponse.Id;
        }

        // If not tags attached, let them know they should add some
        if (thread.AppliedTags.Count == 0)
        {
            var botResponse = await thread.SendMessageAsync(NoAppliedTagsUsed);
            container.NoAppliedTagsMessage = botResponse.Id;
        }

        // TODO : (James) Should we push this into a Task.Run?
        // Sets up the thread to be closed after a certain amount of time (This will quickly be removed if anyone interacts with the thread)
        await StealthDeleteThreadInTime(container);
    }
    
    private async Task GatewayOnThreadCreated(SocketThreadChannel thread)
    {
        if (thread.ParentChannel.Id != _helpChannel.Id)
            return;
        if (thread.Owner.IsBot)
            return;
        // Gateway is called twice for forums, not sure why
        if (_activeThreads.ContainsKey(thread.Id))
            return;
        
        LoggingService.DebugLog($"[UnityHelpService] New Thread Created: {thread.Id} - {thread.Name}", LogSeverity.Debug);
        Task.Run(() => OnThreadCreated(thread));
    }
    
    #endregion // Thread Creation

    #region Thread Update

    private async Task OnThreadUpdated(SocketThreadChannel before, SocketThreadChannel after)
    {
        var thread = _activeThreads[after.Id];

        // If the user was informed to add tags and they add some, we revoke the message
        if (thread.NoAppliedTagsMessage != 0 && before.AppliedTags.Count < after.AppliedTags.Count)
        {
            var message = await after.GetMessageAsync(thread.NoAppliedTagsMessage);
            if (message != null)
                await message.DeleteAsync();
            thread.NoAppliedTagsMessage = 0;
        }

        //! Handle when the thread gets resolve tag added to it
        bool isNewResolved = !before.AppliedTags.Contains(_resolvedForumTag.Id) &&
                             after.AppliedTags.Contains(_resolvedForumTag.Id);
        if (isNewResolved)
        {
            // Posts a message in chat and closes the channel after x minutes
            thread.IsResolved = true;
            if (_activeThreads.TryGetValue(after.Id, out thread))
            {
                await CloseThreadInTime(thread, ResolvedWarnOfPendingCloseMessage, TimeBeforeClosedForResolvedTag);
            }
        }

        // TODO : (James)
        // //! Handle when the thread was resolved, but has been changed to unresolved
        // if (before.AppliedTags.Contains(_resolvedForumTag.Id) &&
        //     !after.AppliedTags.Contains(_resolvedForumTag.Id))
        // {
        //
        // }
    }
    
    private async Task GatewayOnThreadUpdated(Cacheable<SocketThreadChannel, ulong> before, SocketThreadChannel after)
    {
        if (after.ParentChannel.Id != _helpChannel.Id)
            return;
        if (!_activeThreads.TryGetValue(after.Id, out var thread))
            return;

        SocketThreadChannel beforeThread = await before.GetOrDownloadAsync();
        SocketThreadChannel afterThread = after;
        
        LoggingService.DebugLog($"[UnityHelpService] Thread Updated: {after.Id} - {after.Name}", LogSeverity.Debug);
        
        Task.Run(async () => OnThreadUpdated(beforeThread, afterThread));
    }
    
    #endregion // Thread Update

    #region Thread Deleted
    
    private async Task OnThreadDeleted(SocketThreadChannel channel)
    {
        var thread = _activeThreads[channel.Id];
        _activeThreads.Remove(channel.Id);

        // Cancel any pending tasks
        thread.CancellationToken?.Cancel();
    }

    private async Task GatewayOnThreadDeleted(Cacheable<SocketThreadChannel, ulong> threadId)
    {
        if (!_activeThreads.ContainsKey(threadId.Id))
            return;
        
        LoggingService.DebugLog($"[UnityHelpService] Thread Deleted: {threadId.Id}", LogSeverity.Debug);

        Task.Run(async () => OnThreadDeleted(await threadId.GetOrDownloadAsync()));
    }
    
    #endregion // Thread Deleted

    #region User Joins/Leaves Thread
    
    private async Task GatewayOnThreadMemberJoinedThread(SocketThreadUser user)
    {
        if (user.Thread.Id != _helpChannel.Id)
            return;

        if (!_activeThreads.TryGetValue(user.Thread.Id, out var thread))
            return;

        thread.HasInteraction = true;
    }

    private async Task GatewayOnThreadMemberLeftThread(SocketThreadUser user)
    {
        if (user.Thread.Id != _helpChannel.Id)
            return;
        
        // TODO : (James) Check if user was author? If so, close thread?
    }
    
    #endregion // User Joins/Leaves Thread

    #region Message Received
    
    private async Task OnMessageReceived(SocketMessage message)
    {
        var thread = _activeThreads[message.Channel.Id];
        
        thread.LatestMessage = message.Id;
        // If Author is only one who has interacted with the thread, we don't need to update anything else
        if (!thread.HasInteraction && message.Author.Id == thread.Owner)
            return;
        
        thread.HasInteraction = true;
        
        var channel = message.Channel as SocketThreadChannel;
        if (channel.Owner.Id == message.Author.Id)
        {
            await RequestThreadShutdownInTime(thread, HasResponseIdleTimeSelfUserMessage + HasResponseExtraMessage, HasResponseIdleTimeSelfUser);
            return;
        }
        else
        {
            await RequestThreadShutdownInTime(thread, HasResponseMessageRequestClose + HasResponseExtraMessage, HasResponseIdleTimeOtherUser);
            return;
        }
    }
    
    private async Task GatewayOnMessageReceived(SocketMessage message)
    {
        if (message.Channel.Id != _helpChannel.Id)
            return;
        if (message.Author.IsBot)
            return;
        if (!_activeThreads.TryGetValue(message.Channel.Id, out var thread))
            return;
        
        LoggingService.DebugLog($"[UnityHelpService] Help Message Received: {message.Id} - {message.Content}", LogSeverity.Debug);
        Task.Run(() => OnMessageReceived(message));
    }

    private async Task OnMessageUpdated(IMessage before, IMessage after, SocketThreadChannel channel)
    {
        var thread = _activeThreads[channel.Id];
        
        if (thread.WarningMessage != 0 && before.Id == thread.FirstMessage)
        {
            if (after.Content.Length > MinimumLengthMessage)
            {
                var warningMessage = await channel.GetMessageAsync(thread.WarningMessage);
                if (warningMessage != null)
                    await warningMessage.DeleteAsync();
                thread.WarningMessage = 0;
            }
        }
    }
    
    private async Task GatewayOnMessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
    {
        if (channel is not SocketThreadChannel)
            return;
        if (((SocketThreadChannel)channel).ParentChannel.Id != _helpChannel.Id)
            return;
        if (after.Author.IsBot)
            return;
        if (!_activeThreads.TryGetValue(channel.Id, out var thread))
            return;
        if (thread.Owner != after.Author.Id)
            return;
        
        var beforeMsg = await before.GetOrDownloadAsync();
        if (beforeMsg == null)
            return;

        LoggingService.DebugLog($"[UnityHelpService] Help Message Updated: {after.Id} - {after.Content}", LogSeverity.Debug);
        Task.Run(() => OnMessageUpdated(beforeMsg, after, channel as SocketThreadChannel));
    }

    #endregion // Message Received
    
    #endregion // Thread Tracking

    #region Event Handlers

    private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> messageCache, Cacheable<IMessageChannel, ulong> channelChache, SocketReaction reaction)
    {
        // Check if reaction is in a proper thread/forum and not just a normal message
        if (await channelChache.GetOrDownloadAsync() is not SocketThreadChannel channel || channel.Id != _helpChannel.Id)
            return;
        var message = await messageCache.GetOrDownloadAsync();
        // Limit locking to only reactions on the bots messages
        if (message == null || message.Author.Id != _client.CurrentUser.Id)
            return;

        Task.Run(async () =>
        {
            // Check the owner is the one reacting
            var threadOwner = channel.Owner.Id;
            if (reaction.UserId != threadOwner)
                return;

            await CloseThread(channel, true);
        });
    }

    #endregion // Event Handlers

    #region Bulk Behaviour Handler

        private async Task CloseThreadInTime(ThreadContainer thread, string message, int minutes)
    {
        if (!(await IsValidThread(thread)))
            return;
        
        var expectedShutdownTime = DateTime.Now.AddMinutes(minutes);
        var threadChannel = _client.GetChannel(thread.ChannelId) as SocketThreadChannel;
        // Check if token already created, each thread shares its own token with any relevant action (close, delete, etc)
        await CancelPreviousWarning(thread, expectedShutdownTime);
        
        thread.CancellationToken ??= new CancellationTokenSource();
        // Send our message
        if (!string.IsNullOrEmpty(message))
        {
            await threadChannel.SendMessageAsync(message);
        }
        thread.ExpectedShutdownTime = expectedShutdownTime;
        await Task.Delay(minutes * 60 * 1000, thread.CancellationToken.Token);
        if (await IsTaskCancelled(thread))
            return;

        await CloseThread(threadChannel!, thread.IsResolved);
    }

    private async Task RequestThreadShutdownInTime(ThreadContainer thread, string msgString, int minutes)
    {
        if (!(await IsValidThread(thread)))
            return;
        
        var expectedWarnTime = DateTime.Now.AddMinutes(minutes);
        var threadChannel = _client.GetChannel(thread.ChannelId) as SocketThreadChannel;
        // Check if token already created, each thread shares its own token with any relevant action (close, delete, etc)
        await CancelPreviousWarning(thread, expectedWarnTime);
        thread.CancellationToken ??= new CancellationTokenSource();
        
        thread.ExpectedShutdownTime = expectedWarnTime;
        await Task.Delay(minutes * 60 * 1000, thread.CancellationToken.Token);
        if (await IsTaskCancelled(thread))
            return;
        
        msgString = string.Format(msgString, threadChannel.Owner.Mention);
        var sentMessage = await threadChannel.SendMessageAsync(msgString);
        // add the lock reaction
        await sentMessage.AddReactionAsync(CloseEmoji);
        thread.LatestMessage = sentMessage.Id;
    }
    
    /// <summary>
    /// When a thread is first started, this is called first to set it up to be closed after a certain amount of time
    /// This will quickly be canceled if the thread is interacted with.
    /// </summary>
    private async Task StealthDeleteThreadInTime(ThreadContainer thread)
    {
        if (!(await IsValidThread(thread)))
            return;
        
        var expectedShutdownTime = DateTime.Now.AddMinutes(NoResponseNotResolvedIdleTime);
        var threadChannel = _client.GetChannel(thread.ChannelId) as SocketThreadChannel;

        thread.CancellationToken ??= new CancellationTokenSource();
        thread.ExpectedShutdownTime = expectedShutdownTime;
        // Wait for the time to pass
        await Task.Delay(NoResponseNotResolvedIdleTime * 60 * 1000, thread.CancellationToken.Token);
        if (await IsTaskCancelled(thread))
            return;

        // We prompt chat that the thread is going to be deleted in x number of hours, which will double as a bump.
        var botResponse = await threadChannel.SendMessageAsync(StealthDeleteMessage);
        thread.BotsLastMessage = botResponse.Id;
        
        // Wait for the next set of time to pass
        thread.ExpectedShutdownTime = DateTime.Now.AddMinutes(StealthDeleteTime);
        await Task.Delay(StealthDeleteTime * 60 * 1000, thread.CancellationToken.Token);
        if (await IsTaskCancelled(thread))
            return;

        await threadChannel.DeleteAsync();
    }

    #endregion // Bulk Behaviour Handler
    
    
    #region Generic Methods
    
    private async Task CloseThread(IThreadChannel channel, bool includeResolvedTag = false)
    {
        var appliedTags = channel.AppliedTags.ToList();
        if (includeResolvedTag && !appliedTags.Contains(_resolvedForumTag.Id))
            appliedTags.Add(_resolvedForumTag.Id);
        
        await channel.ModifyAsync(x =>
        {
            x.Archived = true;
            x.AppliedTags = appliedTags;
        });

        await EndThreadTracking(channel.Id);
    }

    private async Task EndThreadTracking(ThreadContainer thread)
    {
        thread.CancellationToken?.Cancel();
        _activeThreads.Remove(thread.ChannelId);
    }
    private async Task EndThreadTracking(ulong threadId)
    {
        if (_activeThreads.TryGetValue(threadId, out var thread))
            await EndThreadTracking(thread);
    }

    // Check if token already created, each thread shares its own token with any relevant action (close, delete, etc)
    private async Task CancelPreviousWarning(ThreadContainer thread, DateTime newShutdownTime)
    {
        if (thread.CancellationToken != null)
        {
            if (thread.ExpectedShutdownTime < newShutdownTime)
            {
                thread.CancellationToken.Cancel();
                thread.CancellationToken = null;
            }
            await RemoveContainerPreviousComment(thread);
        }
    }
    
    private async Task<List<IThreadChannel>> GetHelpActiveThreads()
    {
        var messages = await _helpChannel.GetActiveThreadsAsync();
        var helpThreads = messages.Where(x => x.CategoryId == _helpChannel.Id).ToList();
        return helpThreads;
    }

    #endregion // Generic Methods

    #region Utility Methods

    private async Task RemoveContainerPreviousComment(ThreadContainer thread)
    {
        if (thread.BotsLastMessage == 0)
            return;
        if (await _client.GetChannelAsync(thread.ChannelId) is SocketThreadChannel threadChannel)
        {
            var message = await threadChannel.GetMessageAsync(thread.BotsLastMessage);
            if (message != null)
            {
                await message.DeleteAsync();
            }
        }
    }

    private async Task<bool> IsValidThread(ThreadContainer thread)
    {
        var threadChannel = _client.GetChannel(thread.ChannelId) as SocketThreadChannel;
        // If channel is null, we have problems, we remove them from _activeThreads, cancel the token and return to avoid an exception
        if (threadChannel == null)
        {
            await EndThreadTracking(thread);
            return false;
        }
        return true;
    }
    
    private async Task<bool> IsTaskCancelled(ThreadContainer thread)
    {
        if (thread.CancellationToken == null)
            return false;
        if (thread.CancellationToken.IsCancellationRequested)
        {
            LoggingService.DebugLog($"[UnityHelpService] Task cancelled for Channel {thread.ChannelId}");
            return true;
        }
        return false;
    }

    #endregion // Utility Methods
    
}